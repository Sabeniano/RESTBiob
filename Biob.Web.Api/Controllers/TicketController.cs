using Biob.Data.Models;
using Biob.Services.Data.Repositories;
using Biob.Services.Web.PropertyMapping;
using Biob.Web.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biob.Services.Data.DtoModels.TicketDtos;
using Microsoft.AspNetCore.JsonPatch;
using AutoMapper;
using Biob.Services.Data.Helpers;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using Biob.Web.Api.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace Biob.Web.Api.Controllers
{
    [Route("/api/v1/movies/{movieId}/showtimes/{showtimeId}/tickets")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IShowtimeRepository _showtimeRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<TicketController> _logger;

        public TicketController(ITicketRepository ticketRepository,
                                IShowtimeRepository showtimeRepository,
                                ISeatRepository seatRepository,
                                IPropertyMappingService propertyMappingService,
                                ITypeHelperService typeHelperService,
                                IUrlHelper urlHelper,
                                ILogger<TicketController> logger)
        {
            _ticketRepository = ticketRepository;
            _showtimeRepository = showtimeRepository;
            _seatRepository = seatRepository;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
            _urlHelper = urlHelper;
            _logger = logger;
            _propertyMappingService.AddPropertyMapping<TicketDto, Ticket>(new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new PropertyMappingValue(new List<string>() { "Id" })},
                { "CustomerId", new PropertyMappingValue(new List<string>() { "CustomerId" })},
                { "ShowtimeId", new PropertyMappingValue(new List<string>() { "ShowtimeId" })},
                { "SeatId", new PropertyMappingValue(new List<string>() { "SeatId" })},
                { "Paid", new PropertyMappingValue(new List<string>() { "Paid" })},
                { "Price", new PropertyMappingValue(new List<string>() { "Price" })},
            });
        }

        [SwaggerOperation(
            Summary = "Retrieve all tickets for a showtime",
            Description = "Retrieves all tickets for a showtime in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every movie", typeof(TicketDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet(Name = "GetTickets")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> GetAllTickets([FromRoute, SwaggerParameter(Description = "Id of the related movie", Required = true)] Guid movieId, 
                                                        [FromRoute, SwaggerParameter(Description = "Id of the related showtime", Required = true)] Guid showtimeId, 
                                                        [FromQuery, SwaggerParameter(Description = "Parameters for the request: PageNumber, OrderBy, Fields, SearchQuery, IncludeMetadata")] RequestParameters requestParameters,
                                                        [FromHeader(Name = "Accept"), SwaggerParameter(Description = "Media type to return eg. json or json+hateoas")] string mediaType)
        {
            if (!await _ticketRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }

            //Note: In TicketRepository, ApplySort is temprarily outcommented as it caused an error somehow. TODO: Could be fixed at some point
            //EDIT: This was becuase it didn't like to sort on a date? now sorting on price to make it work. 
            //TODO: Change back to CreatedOn
            if (string.IsNullOrWhiteSpace(requestParameters.OrderBy))
            {
                requestParameters.OrderBy = "Price";
            }

            if (!_propertyMappingService.ValidMappingExistsFor<TicketDto, Ticket>(requestParameters.Fields))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<TicketDto>(requestParameters.Fields))
            {
                return BadRequest();
            }

            PagedList<Ticket> ticketsPagedList = await _ticketRepository.GetAllTicketsByShowtimeIdAsync(showtimeId, requestParameters.OrderBy, requestParameters.SearchQuery,
                                                                                                        requestParameters.PageNumber, requestParameters.PageSize);

            var tickets = Mapper.Map<IEnumerable<TicketDto>>(ticketsPagedList);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                return Ok(CreateHateoasResponse(ticketsPagedList, requestParameters));
            }
            else
            {
                var previousPageLink = ticketsPagedList.HasPrevious ? CreateUrlForResource(requestParameters, PageType.PreviousPage) : null;
                var nextPageLink = ticketsPagedList.HasNext ? CreateUrlForResource(requestParameters, PageType.NextPage) : null;
                var paginationMetadata = new PaginationMetadata()
                {
                    TotalCount = ticketsPagedList.TotalCount,
                    PageSize = ticketsPagedList.PageSize,
                    CurrentPage = ticketsPagedList.CurrentPage,
                    TotalPages = ticketsPagedList.TotalPages,
                    PreviousPageLink = previousPageLink,
                    NextPageLink = nextPageLink
                };

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                if (requestParameters.IncludeMetadata)
                {
                    var shapedTickets = tickets.ShapeData(requestParameters.Fields);
                    var ticketsWithMetadata = new EntityWithPaginationMetadataDto<ExpandoObject>(paginationMetadata, shapedTickets);
                    return Ok(ticketsWithMetadata);
                }

                return Ok(tickets.ShapeData(requestParameters.Fields));
            }
        }

        [SwaggerOperation(
            Summary = "Get a ticket by ID",
            Description = "Retrieves a ticket in the database with the provided id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a movie", typeof(TicketDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{ticketId}", Name = "GetTicket")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId", "ticketId" })]
        public async Task<IActionResult> GetOneTicket([FromRoute, SwaggerParameter(Description = "Id of the related movie", Required = true)] Guid movieId, 
                                                        [FromRoute, SwaggerParameter(Description = "Id of the related showtime", Required = true)]Guid showtimeId, 
                                                        [FromRoute, SwaggerParameter(Description = "Id of the ticket to get", Required = true)]Guid ticketId, 
                                                        [FromQuery, SwaggerParameter(Description = "Specification of what ticket fields to return")] string fields,
                                                        [FromHeader(Name = "Accept"), SwaggerParameter(Description = "Media type to return eg. json or json+hateoas")] string mediaType)
        {

            if (!await _ticketRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.TicketExists(ticketId))
            {
                return NotFound();
            }

            var foundTicket = await _ticketRepository.GetTicketAsync(ticketId);


            var ticket = Mapper.Map<TicketDto>(foundTicket);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForTickets(ticketId, fields);

                var linkedTicket = ticket.ShapeData(fields) as IDictionary<string, object>;
                linkedTicket.Add("links", links);
                return Ok(linkedTicket);
            }
            else
            {
                return Ok(foundTicket.ShapeData(fields));
            }
        }

        [SwaggerOperation(
           Summary = "Create a ticket",
           Description = "creates a ticket in the database",
           Consumes = new string[] { "application/json" },
           Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a ticket", typeof(TicketDto))]
        [HttpPost(Name = "CreateTicket")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId"})]
        public async Task<IActionResult> CreateTicketAsync([FromBody, SwaggerParameter(Description = "Values of the ticket to be created eg. which seat and what price", Required = true)] TicketToCreateDto ticketToCreateDto, 
                                                           [FromHeader(Name = "Accept"), SwaggerParameter(Description = "Media type to return eg. json or json+hateoas")] string mediaType,
                                                           [FromRoute, SwaggerParameter(Description = "Id of the related movie", Required = true)] Guid movieId, 
                                                           [FromRoute, SwaggerParameter(Description = "Id of the related showtime", Required = true)] Guid showtimeId)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }




            if (ticketToCreateDto.SeatId == Guid.Empty)
            {
                return BadRequest();
                //var availableSeat = await _seatRepository.GetFirstAvailableSeatByShowtimeIdAsync(showtimeId);
                //if (availableSeat == null)
                //{
                //    //Not sure if this is enough. Could be tested. 
                //    _logger.LogError("No available seats");

                //}
                //ticketToCreateDto.SeatId = availableSeat.Id;
            }

            var ticket = Mapper.Map<Ticket>(ticketToCreateDto);


            ticket.ShowtimeId = showtimeId;

            ticket.Id = Guid.NewGuid();

            _ticketRepository.AddTicket(ticket);

            if (!await _ticketRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a showtime failed");
            }

            var ticketDto = Mapper.Map<TicketDto>(ticket);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForTickets(ticketDto.Id, null);

                var linkedTicket = ticketDto.ShapeData(null) as IDictionary<string, object>;

                linkedTicket.Add("links", links);

                return CreatedAtRoute("GetTicket", new { ticketId = ticketDto.Id }, linkedTicket);
            }
            else
            {
                //Hmm why is dto used here when it's not in the other methods?
                return CreatedAtRoute("GetTicket", new { ticketId = ticketDto.Id }, ticketDto);
            }
        }

        [SwaggerOperation(
            Summary = "Update a ticket",
            Description = "Updates a ticket in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a ticket", typeof(TicketDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{ticketId}", Name = "UpdateTicket")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId", "ticketId" })]
        public async Task<IActionResult> UpdateTicket([FromRoute, SwaggerParameter(Description = "Id of ticket to update", Required = true)] Guid ticketId, 
                                                        [FromRoute, SwaggerParameter(Description = "Id of related movie", Required = true)] Guid movieId, 
                                                        [FromRoute, SwaggerParameter(Description = "Id of related showtime", Required = true)] Guid showtimeId,
                                                        [FromBody, SwaggerParameter(Description = "Fields to update and their new value", Required = true)] TicketToUpdateDto ticketToUpdate, 
                                                        [FromHeader(Name = "Accept"), SwaggerParameter(Description = "Media type to return eg. json or json+hateoas")] string mediaType)
        {
            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }


            var ticketFromDb = await _ticketRepository.GetTicketAsync(ticketId);

            //  upserting if ticket does not already exist
            if (ticketFromDb == null)
            {
                var ticketEntity = Mapper.Map<Ticket>(ticketToUpdate);
                ticketEntity.Id = ticketId;
                ticketEntity.ShowtimeId = showtimeId;

                var availableSeat = await _seatRepository.GetFirstAvailableSeatByShowtimeIdAsync(showtimeId);
                ticketEntity.SeatId = availableSeat.Id;


                _ticketRepository.AddTicket(ticketEntity);

                if (!await _ticketRepository.SaveChangesAsync())
                {
                    _logger.LogError("Failed to upsert a new ticket");
                }

                var ticketToReturn = Mapper.Map<TicketDto>(ticketEntity);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForTickets(ticketToReturn.Id, null);

                    var linkedTicket = ticketToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedTicket.Add("links", links);

                    return CreatedAtRoute("GetTicket", new { ticketId = ticketToReturn.Id }, linkedTicket);
                }
                else
                {
                    return CreatedAtRoute("GetTicket", new { ticketId = ticketToReturn.Id }, ticketToReturn);
                }
            }

            Mapper.Map(ticketToUpdate, ticketFromDb);

            _ticketRepository.UpdateTicket(ticketFromDb);

            if (!await _ticketRepository.SaveChangesAsync())
            {
                _logger.LogError("Failed to update a ticket");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Partially update a movie",
            Description = "Partially updates a movie in the database",
            Consumes = new string[] { "application/json-patch+json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully partially updated a movie", typeof(TicketDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{ticketId}", Name = "PartiallyUpdateTicket")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId", "ticketId" })]
        public async Task<IActionResult> PartiallyUpdateTicket([FromRoute, SwaggerParameter(Description = "Id of ticket to update", Required = true)] Guid ticketId,
                                                               [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<TicketToUpdateDto> patchDoc,
                                                               [FromRoute, SwaggerParameter(Description = "Id of the related movie", Required = true)] Guid movieId, 
                                                               [FromRoute, SwaggerParameter(Description = "Id of the related showtime", Required = true)] Guid showtimeId,
                                                               [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _ticketRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }

            if (patchDoc == null)
            {
                return BadRequest();
            }

            var ticketFromDb = await _ticketRepository.GetTicketAsync(ticketId);


            if (ticketFromDb == null)
            {
                var ticketToCreate = new TicketToUpdateDto();

                patchDoc.ApplyTo(ticketToCreate, ModelState);

                if (!ModelState.IsValid)
                {
                    new ProccessingEntityObjectResultErrors(ModelState);
                }

                var ticketToAddToDb = Mapper.Map<Ticket>(ticketToCreate);
                ticketToAddToDb.Id = ticketId;

                _ticketRepository.AddTicket(ticketToAddToDb);

                if (!await _ticketRepository.SaveChangesAsync())
                {
                    _logger.LogError("Upserting ticket failed");
                }

                var ticketToReturn = Mapper.Map<TicketDto>(ticketToAddToDb);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForTickets(ticketToReturn.Id, null);

                    var linkedTicket = ticketToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedTicket.Add("links", links);

                    return CreatedAtRoute("GetTicket", new { ticketId = ticketToReturn.Id }, linkedTicket);
                }
                else
                {
                    return CreatedAtRoute("GetTicket", new { ticketId = ticketToReturn.Id }, ticketToReturn);
                }

                
            }

            var ticketToPatch = Mapper.Map<TicketToUpdateDto>(ticketFromDb);

            patchDoc.ApplyTo(ticketToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                new ProccessingEntityObjectResultErrors(ModelState);
            }

            Mapper.Map(ticketToPatch, ticketFromDb);

            _ticketRepository.UpdateTicket(ticketFromDb);

            if (!await _ticketRepository.SaveChangesAsync())
            {
                _logger.LogError("partially updating ticket failed");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Soft deletes a ticket",
            Description = "Soft deletes a ticket in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully deleted a ticket", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{ticketId}", Name = "DeleteTicket")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId", "ticketId" })]
        public async Task<IActionResult> DeleteTicketAsync([FromRoute, SwaggerParameter(Description = "If of the related movie", Required = true)] Guid movieId, 
                                                            [FromRoute, SwaggerParameter(Description = "Id of the related showtime", Required = true)] Guid showtimeId, 
                                                            [FromRoute, SwaggerParameter(Description = "Id of the ticket to delete", Required = true)] Guid ticketId)
        {
            if (!await _ticketRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!await _ticketRepository.ShowtimeExists(showtimeId))
            {
                return NotFound();
            }

            var ticketFromDb = await _ticketRepository.GetTicketAsync(ticketId);

            if (ticketFromDb == null)
            {
                return NotFound();
            }

            _ticketRepository.DeleteTicket(ticketFromDb);

            if (!await _ticketRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting ticket: {ticketId} failed on save");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Get option information",
            Description = "Gets HTTP methods options for this route",
            Consumes = new string[] { },
            Produces = new string[] { })]
        [HttpOptions]
        public IActionResult GetTicketsOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        }

        [SwaggerOperation(
            Summary = "Get option information",
            Description = "Gets HTTP methods options for this route",
            Consumes = new string[] { },
            Produces = new string[] { })]
        [HttpOptions("{ticketId}")]
        public IActionResult GetTicketOptions()
        {
            Response.Headers.Add("Allow", "GET, PUT, PATCH, OPTIONS");
            return Ok();
        }

        private ExpandoObject CreateHateoasResponse(PagedList<Ticket> ticketsPagedList, RequestParameters requestParameters)
        {

            var tickets = Mapper.Map<IEnumerable<TicketDto>>(ticketsPagedList);

            var paginationMetadataWithLinks = new
            {
                ticketsPagedList.TotalCount,
                ticketsPagedList.PageSize,
                ticketsPagedList.CurrentPage,
                ticketsPagedList.TotalPages
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadataWithLinks));

            var links = CreateLinksForTickets(requestParameters, ticketsPagedList.HasNext, ticketsPagedList.HasPrevious);

            var shapedTickets = tickets.ShapeData(requestParameters.Fields);

            var shapedTicketsWithLinks = shapedTickets.Select(ticket =>
            {
                var ticketDictionary = ticket as IDictionary<string, object>;
                var ticketLinks = CreateLinksForTickets((Guid)ticketDictionary["Id"], requestParameters.Fields);

                ticketDictionary.Add("links", ticketLinks);

                return ticketDictionary;
            });
            if (requestParameters.IncludeMetadata)
            {
                var ticketsWithMetadata = new ExpandoObject();
                ((IDictionary<string, object>)ticketsWithMetadata).Add("Metadata", paginationMetadataWithLinks);
                ((IDictionary<string, object>)ticketsWithMetadata).Add("tickets", shapedTicketsWithLinks);
                ((IDictionary<string, object>)ticketsWithMetadata).Add("links", links);
                return ticketsWithMetadata;
            }
            else
            {
                var linkedCollection = new ExpandoObject();
                ((IDictionary<string, object>)linkedCollection).Add("tickets", shapedTicketsWithLinks);
                ((IDictionary<string, object>)linkedCollection).Add("links", links);
                return linkedCollection;
            }

        }

        private IEnumerable<LinkDto> CreateLinksForTickets(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(_urlHelper.Link("GetTicket", new { ticketId = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(_urlHelper.Link("GetTicket", new { ticketId = id, fields }), "self", "GET"));
            }
            links.Add(
                new LinkDto(_urlHelper.Link("DeleteTicket", new { ticketId = id }), "delete_ticket", "DELETE")
                );
            links.Add(
                new LinkDto(_urlHelper.Link("UpdateTicket", new { ticketId = id }), "update_ticket", "PUT")
                );
            links.Add(
                new LinkDto(_urlHelper.Link("PartiallyUpdateTicket", new { ticketId = id }), "partially_update_ticket", "PATCH")
                );
            links.Add(
                new LinkDto(_urlHelper.Link("CreateTicket", new { ticketId = id }), "create_ticket", "POST")
                );
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForTickets(RequestParameters requestParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.Current), "self", "GET")
            };

            if (hasNext)
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.NextPage), "self", "GET");
            }

            if (hasPrevious)
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.PreviousPage), "self", "GET");
            }

            return links;
        }

        private string CreateUrlForResource(RequestParameters requestParameters, PageType pageType)
        {
            switch (pageType)
            {
                case PageType.PreviousPage:
                    return _urlHelper.Link("GetTickets", new
                    {
                        fields = requestParameters.Fields,
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber - 1,
                        pageSize = requestParameters.PageSize

                    });
                case PageType.NextPage:
                    return _urlHelper.Link("GetTickets", new
                    {
                        fields = requestParameters.Fields,
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber + 1,
                        pageSize = requestParameters.PageSize
                    });
                default:
                    return _urlHelper.Link("GetTickets", new
                    {
                        fields = requestParameters.Fields,
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber,
                        pageSize = requestParameters.PageSize
                    });
            }
        }
    }
}

