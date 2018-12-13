using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Biob.Services.Data.Repositories;
using AutoMapper;
using Biob.Web.Api.Helpers;
using Biob.Data.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using Biob.Services.Data.Helpers;
using System.Dynamic;
using System.Linq;
using Biob.Services.Data.DtoModels.SeatDtos;
using Biob.Web.Api.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace Biob.Web.Api.Controllers
{
    [Route("api/v1/halls/{hallId}/seats")]
    [ApiController]
    public class SeatController : ControllerBase
    {
        private readonly ISeatRepository _seatRepository;
        private readonly ILogger<SeatController> _logger;
        private readonly IUrlHelper _urlHelper;
       

        public SeatController(ISeatRepository seatRepository, ILogger<SeatController> logger,
                              IUrlHelper urlHelper)
        {
            _seatRepository = seatRepository;
            _logger = logger;
            _urlHelper = urlHelper;
        }

        [SwaggerOperation(
            Summary = "Retrieve every seat",
            Description = "Retrieves every seat in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every seat", typeof(SeatDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet(Name = "GetSeats")]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> GetAllSeatsAsync(
            [FromRoute] Guid hallId,
            [FromQuery]RequestParameters requestParameters,
            [FromHeader(Name = "Accept")] string mediaType)
        {
            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(requestParameters.Fields))
            {
                return BadRequest();
            }

            var seatsPagedList = await _seatRepository.GetAllSeatsByHallIdAsync(hallId, requestParameters.PageNumber, requestParameters.PageSize);

            var seats = Mapper.Map<IEnumerable<SeatDto>>(seatsPagedList);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                return Ok(CreateHateoasResponse(seatsPagedList, requestParameters));
            }
            else
            {
                var previousPageLink = seatsPagedList.HasPrevious ? CreateUrlForResource(requestParameters, PageType.PreviousPage) : null;
                var nextPageLink = seatsPagedList.HasNext ? CreateUrlForResource(requestParameters, PageType.NextPage) : null;
                var paginationMetadata = new PaginationMetadata()
                {
                    TotalCount = seatsPagedList.TotalCount,
                    PageSize = seatsPagedList.PageSize,
                    CurrentPage = seatsPagedList.CurrentPage,
                    TotalPages = seatsPagedList.TotalPages,
                    PreviousPageLink = previousPageLink,
                    NextPageLink = nextPageLink
                };

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                if (requestParameters.IncludeMetadata)
                {
                    var seatsWithMetadata = new EntityWithPaginationMetadataDto<SeatDto>(paginationMetadata, seats);
                    return Ok(seatsWithMetadata);
                }

                return Ok(seats);
            }
        }

        [SwaggerOperation(
            Summary = "Retrieve one seat by ID",
            Description = "Retrieves seat in the database by id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a seat", typeof(SeatDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{seatId}", Name = "GetSeat")]
        [GuidCheckActionFilter(new string[] { "seatId", "hallId" })]
        public async Task<IActionResult> GetOneSeatAsync(
            [FromRoute, SwaggerParameter(Description = "the hall ID to find seat by", Required = true)] Guid hallId,
            [FromRoute, SwaggerParameter(Description = "the ID to find seat by", Required = true)] Guid seatId,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }

            var foundSeat = await _seatRepository.GetSeatAsync(seatId);

            if (foundSeat == null)
            {
                return NotFound();
            }

            var seatToReturn = Mapper.Map<SeatDto>(foundSeat);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForSeats(seatId, null);

                var linkedSeat = seatToReturn.ShapeData(null) as IDictionary<string, object>;
                linkedSeat.Add("links", links);
                return Ok(linkedSeat);
            }
            else
            {
                return Ok(seatToReturn.ShapeData(null));
            }
        }

        [SwaggerOperation(
            Summary = "Create a seat",
            Description = "creates a seat in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a seat", typeof(SeatDto))]
        [HttpPost]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> CreateSeatAsync(
            [FromRoute, SwaggerParameter(Description = "the hall ID to create seat by", Required = true)] Guid hallId,
            [FromBody, SwaggerParameter(Description = "Seats to create", Required = true)] SeatToCreateDto seatToCreate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }
            
            var seatToAdd = Mapper.Map<Seat>(seatToCreate);
            seatToAdd.Id = Guid.NewGuid();

            _seatRepository.AddSeat(hallId, seatToAdd);

            if (!await _seatRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a seat failed");
            }

            var seat = Mapper.Map<Seat>(seatToAdd);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForSeats(seat.Id, null);

                var linkedSeat = seat.ShapeData(null) as IDictionary<string, object>;
                linkedSeat.Add("links", links);

                return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToAdd.Id }, linkedSeat);
            }
            else
            {
                return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToAdd.Id }, seat);
            }
        }

        [SwaggerOperation(
            Summary = "Update a seat",
            Description = "Updates a seat in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a seat", typeof(SeatDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{seatId}", Name = "UpdateSeat")]
        [GuidCheckActionFilter(new string[] { "seatId", "hallId" })]
        public async Task<IActionResult> UpdateSeatByIdAsync(
            [FromRoute, SwaggerParameter(Description = "Hall id of seat to update", Required = true)] Guid hallId,
            [FromRoute, SwaggerParameter(Description = "Id of seat to update", Required = true)] Guid seatId,
            [FromBody, SwaggerParameter(Description = "Seat to update", Required = true)] SeatToUpdateDto seatToUpdate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }

            var seatFromDb = await _seatRepository.GetSeatAsync(seatId);

            // upserting if movie does not already exist
            if (seatFromDb == null)
            {
                var seatEntity = Mapper.Map<Seat>(seatToUpdate);
                seatEntity.Id = seatId;
                _seatRepository.AddSeat(hallId, seatEntity);

                if (!await _seatRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting seat: {seatId} failed on save");
                }

                var seatToReturn = Mapper.Map<SeatDto>(seatEntity);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForSeats(seatToReturn.Id, null);

                    var linkedSeat = seatToReturn.ShapeData(null) as IDictionary<string, object>;
                    linkedSeat.Add("links", links);

                    return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToReturn.Id }, linkedSeat);
                }
                else
                {
                    return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToReturn.Id }, seatToReturn);
                }
            }

            Mapper.Map(seatToUpdate, seatFromDb);

            _seatRepository.UpdateSeat(seatFromDb);

            if (!await _seatRepository.SaveChangesAsync())
            {
                _logger.LogError($"Updating seat: {seatId} failed on save");
            }

            return NoContent();

        }

        [SwaggerOperation(
            Summary = "Partially update a seat",
            Description = "Partially updates a seat in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a seat", typeof(SeatDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{seatId}", Name = "PartiallyUpdateSeat")]
        [GuidCheckActionFilter(new string[] { "seatId", "hallId" })]
        public async Task<IActionResult> PartiuallyUpdateSeatByIdAsync(
            [FromRoute, SwaggerParameter(Description = "ID of seat to update", Required = true)] Guid hallId,
            [FromRoute, SwaggerParameter(Description = "ID of hall to update seat", Required = true)] Guid seatId,
            [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<SeatToUpdateDto> patchDoc,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }

            if (patchDoc == null)
            {
                return BadRequest();
            }

            var seatFromDb = await _seatRepository.GetSeatAsync(seatId);

            //  upserting if movie does not already exist
            if (seatFromDb == null)
            {
                var seatToCreate = new SeatToUpdateDto();
                patchDoc.ApplyTo(seatToCreate, ModelState);

                if (!ModelState.IsValid)
                {
                    new ProccessingEntityObjectResultErrors(ModelState);
                }

                var seatToAddToDb = Mapper.Map<Seat>(seatToCreate);
                seatToAddToDb.Id = seatId;
                _seatRepository.AddSeat(hallId, seatToAddToDb);

                if (!await _seatRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting seat: {seatId} failed on save");
                }

                var seatToReturn = Mapper.Map<SeatDto>(seatToAddToDb);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForSeats(seatToReturn.Id, null);

                    var linkedSeat = seatToReturn.ShapeData(null) as IDictionary<string, object>;
                    linkedSeat.Add("links", links);

                    return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToReturn.Id }, linkedSeat);
                }
                else
                {
                    return CreatedAtRoute("GetSeat", new { hallId, seatId = seatToReturn.Id }, seatToReturn);
                }
            }

            var seatToPatch = Mapper.Map<SeatToUpdateDto>(seatFromDb);

            patchDoc.ApplyTo(seatToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                new ProccessingEntityObjectResultErrors(ModelState);
            }

            Mapper.Map(seatToPatch, seatFromDb);
            _seatRepository.UpdateSeat(seatFromDb);

            if (!await _seatRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating seat: {seatId} failed on save");
            }

            return NoContent();
        }

        [SwaggerOperation(
           Summary = "Soft deletes a seat",
           Description = "Soft deletes a seat in the database",
           Consumes = new string[] { },
           Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully deleted a seat", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{seatId}", Name = "DeleteSeat")]
        [GuidCheckActionFilter(new string[] { "seatId", "hallId" })]
        public async Task<IActionResult> DeleteSeatByIdAsync(
            [FromRoute, SwaggerParameter(Description = "Id of hall to delete seat", Required = true)] Guid hallId,
            [FromRoute, SwaggerParameter(Description = "ID of seat to delete", Required = true)] Guid seatId)
        {
            if (!await _seatRepository.HallExists(hallId))
            {
                return NotFound();
            }

            var seatToDelete = await _seatRepository.GetSeatAsync(seatId);

            if (seatToDelete == null)
            {
                return NotFound();
            }

            _seatRepository.DeleteSeat(seatToDelete);

            if (!await _seatRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting seat: {seatId} failed on save");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Get option information",
            Description = "Gets HTTP methods options for this route",
            Consumes = new string[] { },
            Produces = new string[] { })]
        [SwaggerResponse(200, "Successfully returned options in http header", null)]
        [HttpOptions]
        public IActionResult GetSeatsOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,OPTIONS");
            return Ok();
        }

        [SwaggerOperation(
            Summary = "Get option information",
            Description = "Gets HTTP methods options for this route",
            Consumes = new string[] { },
            Produces = new string[] { })]
        [SwaggerResponse(200, "Successfully returned options in http header", null)]
        [HttpOptions("{seatId}")]
        public IActionResult GetSeatOptions()
        {
            Response.Headers.Add("Allow", "GET,PATCH,PUT,OPTIONS");
            return Ok();
        }

        private ExpandoObject CreateHateoasResponse(PagedList<Seat> seatsPagedList, RequestParameters requestParameters)
        {
            var seats = Mapper.Map<IEnumerable<SeatDto>>(seatsPagedList);

            var paginationMetadataWithLinks = new
            {
                seatsPagedList.TotalCount,
                seatsPagedList.PageSize,
                seatsPagedList.CurrentPage,
                seatsPagedList.TotalPages
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadataWithLinks));

            var links = CreateLinksForSeats(requestParameters, seatsPagedList.HasNext, seatsPagedList.HasPrevious);

            var shapedSeats = seats.ShapeData(requestParameters.Fields);

            var shapedSeatsWithLinks = shapedSeats.Select(seat =>
            {
                var seatDictionary = seat as IDictionary<string, object>;
                var seatLinks = CreateLinksForSeats((Guid)seatDictionary["Id"], requestParameters.Fields);

                seatDictionary.Add("links", seatLinks);

                return seatDictionary;
            });
            if (requestParameters.IncludeMetadata)
            {
                var seatsWithMetadata = new ExpandoObject();
                ((IDictionary<string, object>)seatsWithMetadata).Add("Metadata", paginationMetadataWithLinks);
                ((IDictionary<string, object>)seatsWithMetadata).Add("seats", shapedSeatsWithLinks);
                ((IDictionary<string, object>)seatsWithMetadata).Add("links", links);
                return seatsWithMetadata;
            }
            else
            {
                var linkedCollection = new ExpandoObject();
                ((IDictionary<string, object>)linkedCollection).Add("seats", shapedSeatsWithLinks);
                ((IDictionary<string, object>)linkedCollection).Add("links", links);
                return linkedCollection;
            }
        }

        private IEnumerable<LinkDto> CreateLinksForSeats(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(_urlHelper.Link("GetSeat", new { seatId = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(_urlHelper.Link("GetSeat", new { seatId = id, fields }), "self", "GET"));
            }
            links.Add(new LinkDto(_urlHelper.Link("DeleteSeat", new { seatId = id }), "delete_seat", "DELETE"));
            {
                links.Add(new LinkDto(_urlHelper.Link("UpdateSeat", new { seatId = id }), "update_seat", "PUT"));
            }
            links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateSeat", new { seatId = id }), "partially_update_seat", "PATCH"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForSeats(RequestParameters requestParameters, bool hasNext, bool hasPrevious)
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
                    return _urlHelper.Link("GetSeats", new
                    {
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber - 1,
                        pageSize = requestParameters.PageSize

                    });
                case PageType.NextPage:
                    return _urlHelper.Link("GetSeats", new
                    {
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber + 1,
                        pageSize = requestParameters.PageSize
                    });
                default:
                    return _urlHelper.Link("GetSeats", new
                    {
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber,
                        pageSize = requestParameters.PageSize
                    });
            }
        }
    }
}
