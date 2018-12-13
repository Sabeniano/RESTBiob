using AutoMapper;
using Biob.Data.Models;
using Biob.Services.Data.DtoModels.ShowtimeDtos;
using Biob.Services.Data.Helpers;
using Biob.Services.Data.Repositories;
using Biob.Services.Web.PropertyMapping;
using Biob.Web.Api.Filters;
using Biob.Web.Api.Helpers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Biob.Web.Api.Controllers
{
    [Route("/api/v1/movies/{movieId}/showtimes")]
    [ApiController]
    public class ShowtimeController : ControllerBase
    {
        private readonly ILogger<ShowtimeController> _logger;
        private readonly IShowtimeRepository _showtimeRepository;
        private readonly ITypeHelperService _typeHelperService;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;

        public ShowtimeController(IShowtimeRepository showtimeRepository, IPropertyMappingService propertyMappingService, 
                                    ITypeHelperService typeHelperService, IUrlHelper urlHelper, ILogger<ShowtimeController> logger)
        {
            _logger = logger;
            _showtimeRepository = showtimeRepository;
            _typeHelperService = typeHelperService;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;

            _propertyMappingService.AddPropertyMapping<ShowtimeDto, Showtime>(new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new PropertyMappingValue(new List<string>() { "Id" })},
                { "MovieId", new PropertyMappingValue(new List<string>() { "MovieId" })},
                { "HallId", new PropertyMappingValue(new List<string>() { "HallId" })},
                { "TimeOfPlaying", new PropertyMappingValue(new List<string>() { "TimeOfPlaying" })},
                { "ThreeDee", new PropertyMappingValue(new List<string>() { "ThreeDee" })}
            });
        }

        [SwaggerOperation(
            Summary = "Retrieve every showtime",
            Description = "Retrieves every showtime in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every showtime", typeof(ShowtimeDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet(Name = "GetShowtimes")]
        [GuidCheckActionFilter(new string[] { "movieId"})]
        public async Task<IActionResult> GetAllShowtimesAsync([FromRoute] Guid movieId,[FromQuery]RequestParameters requestParameters, [FromHeader(Name = "Accept")] string mediaType)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(requestParameters.OrderBy))
            {
                //  TODO: change to order by when playing
                requestParameters.OrderBy = "Id";
            }

            if (!_propertyMappingService.ValidMappingExistsFor<ShowtimeDto, Showtime>(requestParameters.Fields))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<ShowtimeDto>(requestParameters.Fields))
            {
                return BadRequest();
            }

            var showtimesPagedList = await _showtimeRepository.GetAllShowtimesAsync(movieId, requestParameters.OrderBy, requestParameters.PageNumber, requestParameters.PageSize);

            var showtimes = Mapper.Map<IEnumerable<ShowtimeDto>>(showtimesPagedList);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                return Ok(CreateHateoasResponse(showtimesPagedList, requestParameters));
            }
            else
            {
                var previousPageLink = showtimesPagedList.HasPrevious ? CreateUrlForResource(requestParameters, PageType.PreviousPage) : null;
                var nextPageLink = showtimesPagedList.HasNext ? CreateUrlForResource(requestParameters, PageType.NextPage) : null;
                var paginationMetadata = new PaginationMetadata()
                {
                    TotalCount = showtimesPagedList.TotalCount,
                    PageSize = showtimesPagedList.PageSize,
                    CurrentPage = showtimesPagedList.CurrentPage,
                    TotalPages = showtimesPagedList.TotalPages,
                    PreviousPageLink = previousPageLink,
                    NextPageLink = nextPageLink
                };

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                if (requestParameters.IncludeMetadata)
                {
                    var showtimesWithMetadata = new EntityWithPaginationMetadataDto<ShowtimeDto>(paginationMetadata, showtimes);
                    return Ok(showtimesWithMetadata);
                }

                return Ok(showtimes);
            }
        }

        [SwaggerOperation(
            Summary = "Retrieve one showtime by ID",
            Description = "Retrieves showtime in the database by id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a showtime", typeof(ShowtimeDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{showtimeId}", Name = "GetShowtime")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> GetOneShowtimeAsync(
            [FromRoute, SwaggerParameter(Description ="the ID to find showtime by", Required = true)]Guid showtimeId,
            [FromRoute, SwaggerParameter(Description = "the movie ID to find showtime by", Required = true)]Guid movieId,
            [FromQuery, SwaggerParameter(Description = "fields requested for data shaping", Required = false)] string fields,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (!_typeHelperService.TypeHasProperties<ShowtimeDto>(fields))
            {
                return BadRequest();
            }

            var foundShowtime = await _showtimeRepository.GetShowtimeAsync(showtimeId, movieId);

            var showtime = Mapper.Map<ShowtimeDto>(foundShowtime);

            if (foundShowtime == null)
            {
                return NotFound();
            }

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForShowtimes(showtimeId, fields);

                var linkedShowtime = showtime.ShapeData(fields) as IDictionary<string, object>;
                linkedShowtime.Add("links", links);
                return Ok(linkedShowtime);
            }
            else
            {
                return Ok(showtime.ShapeData(fields));
            }
        }

        [SwaggerOperation(
            Summary = "Create a showtime",
            Description = "creates a showtime in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a showtime", typeof(ShowtimeDto))]
        [HttpPost]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> CreateShowtimeAsync(
            [FromRoute, SwaggerParameter(Description = "the movie ID to create showtime by", Required = true)]Guid movieId,
            [FromBody, SwaggerParameter(Description = "Showtimes to create", Required = true)] ShowtimeToCreateDto showtimeToCreate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            var showtimeToAdd = Mapper.Map<Showtime>(showtimeToCreate);
            showtimeToAdd.Id = Guid.NewGuid();

            _showtimeRepository.AddShowtime(movieId, showtimeToAdd);

            if (!await _showtimeRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a showtime failed");
            }

            var showtime = Mapper.Map<ShowtimeDto>(showtimeToAdd);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForShowtimes(showtime.Id, null);

                var linkedShowtime = showtime.ShapeData(null) as IDictionary<string, object>;

                linkedShowtime.Add("links", links);

                return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToAdd.Id }, linkedShowtime);
            }
            else
            {
                return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToAdd.Id }, showtime);
            }
        }

        [SwaggerOperation(
            Summary = "Update a showtime",
            Description = "Updates a showtime in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a showtime", typeof(ShowtimeDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{showtimeId}", Name = "UpdateShowtime")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> UpdateShowtimeAsync(
            [FromRoute, SwaggerParameter(Description = "Movie id of showtime to update", Required = true)]Guid movieId,
            [FromRoute, SwaggerParameter(Description = "Id of showtime to update", Required = true)] Guid showtimeId,
            [FromBody, SwaggerParameter(Description = "Showtime to update", Required = true)] ShowtimeToUpdateDto showtimeToUpdate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (showtimeToUpdate == null)
            {
                return BadRequest();
            }

            var showtimeFromDb = await _showtimeRepository.GetShowtimeAsync(showtimeId, movieId);

            if (showtimeFromDb == null)
            {
                var showtimeEntity = Mapper.Map<Showtime>(showtimeToUpdate);
                showtimeEntity.Id = showtimeId;
                _showtimeRepository.AddShowtime(movieId, showtimeEntity);

                if (!await _showtimeRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting showtime: {showtimeId} failed on save");
                }

                var showtimeToReturn = Mapper.Map<ShowtimeDto>(showtimeEntity);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForShowtimes(showtimeToReturn.Id, null);

                    var linkedShowtime = showtimeToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedShowtime.Add("links", links);

                    return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToReturn.Id }, linkedShowtime);
                }
                else
                {
                    return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToReturn.Id }, showtimeToReturn);
                }
            }

            Mapper.Map(showtimeToUpdate, showtimeFromDb);

            _showtimeRepository.UpdateShowtime(showtimeFromDb);

            if (!await _showtimeRepository.SaveChangesAsync())
            {
                _logger.LogError($"Updating showtime: {showtimeId} failed on save");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Partially update a showtime",
            Description = "Partially updates a showtime in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a showtime", typeof(ShowtimeDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{showtimeId}", Name = "PartiallyUpdateShowtime")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> PartiuallyUpdateShowtimeAsync(
            [FromRoute, SwaggerParameter(Description = "ID of showtime to update", Required = true)]Guid showtimeId,
            [FromRoute, SwaggerParameter(Description = "ID of movie to update showtime", Required = true)]Guid movieId,
            [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<ShowtimeToUpdateDto> patchDoc,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }

            if (patchDoc == null)
            {
                return BadRequest();
            }

            var showtimeFromDb = await _showtimeRepository.GetShowtimeAsync(showtimeId, movieId);

            if (showtimeFromDb == null)
            {
                var showtimeToCreate = new ShowtimeToUpdateDto();

                patchDoc.ApplyTo(showtimeToCreate, ModelState);

                if (!ModelState.IsValid)
                {
                    new ProccessingEntityObjectResultErrors(ModelState);
                }

                var showtimeToAddToDb = Mapper.Map<Showtime>(showtimeToCreate);
                showtimeToAddToDb.Id = showtimeId;
                _showtimeRepository.AddShowtime(movieId, showtimeToAddToDb);

                if (!await _showtimeRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting showtime: {showtimeId} failed on save");
                }

                var showtimeToReturn = Mapper.Map<ShowtimeDto>(showtimeToAddToDb);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForShowtimes(showtimeToReturn.Id, null);

                    var linkedShowtime = showtimeToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedShowtime.Add("links", links);

                    return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToReturn.Id }, linkedShowtime);
                }
                else
                {
                    return CreatedAtRoute("GetShowtime", new { movieId, showtimeId = showtimeToReturn.Id }, showtimeToReturn);
                }
            }

            var showtimeToPatch = Mapper.Map<ShowtimeToUpdateDto>(showtimeFromDb);

            patchDoc.ApplyTo(showtimeToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                new ProccessingEntityObjectResultErrors(ModelState);
            }

            Mapper.Map(showtimeToPatch, showtimeFromDb);

            _showtimeRepository.UpdateShowtime(showtimeFromDb);

            if (!await _showtimeRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating {showtimeId} failed");
            }

            return NoContent();
        }

        [SwaggerOperation(
           Summary = "Soft deletes a showtime",
           Description = "Soft deletes a showtime in the database",
           Consumes = new string[] { },
           Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully deleted a showtime", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{showtimeId}", Name = "DeleteShowtime")]
        [GuidCheckActionFilter(new string[] { "movieId", "showtimeId" })]
        public async Task<IActionResult> DeleteShowtimeAsync(
            [FromRoute, SwaggerParameter(Description = "Id of showtime to delete", Required = true)] Guid showtimeId,
            [FromRoute, SwaggerParameter(Description = "ID of movie to delete showtime", Required = true)]Guid movieId)
        {

            if (!await _showtimeRepository.MovieExists(movieId))
            {
                return NotFound();
            }


            var showtimeToDelete = await _showtimeRepository.GetShowtimeAsync(showtimeId, movieId);

            if (showtimeToDelete == null)
            {
                return NotFound();
            }

            _showtimeRepository.DeleteShowtime(showtimeToDelete);

            if (!await _showtimeRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting {showtimeId} failed");
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
        public IActionResult GetShowtimesOptions()
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
        [HttpOptions("{showtimeId}")]
        public IActionResult GetShowtimeOptions()
        {
            Response.Headers.Add("Allow", "GET,PATCH,PUT,OPTIONS");
            return Ok();
        }

        private ExpandoObject CreateHateoasResponse(PagedList<Showtime> showtimesPagedList, RequestParameters requestParameters)
        {
            var showtimes = Mapper.Map<IEnumerable<ShowtimeDto>>(showtimesPagedList);

            var paginationMetadataWithLinks = new
            {
                showtimesPagedList.TotalCount,
                showtimesPagedList.PageSize,
                showtimesPagedList.CurrentPage,
                showtimesPagedList.TotalPages
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadataWithLinks));

            var links = CreateLinksForShowtimes(requestParameters, showtimesPagedList.HasNext, showtimesPagedList.HasPrevious);

            var shapedShowtimes = showtimes.ShapeData(requestParameters.Fields);

            var shapedShowtimesWithLinks = shapedShowtimes.Select(showtime =>
           {
               var showtimeDictionary = showtime as IDictionary<string, object>;
               var showtimeLinks = CreateLinksForShowtimes((Guid)showtimeDictionary["Id"], requestParameters.Fields);

               showtimeDictionary.Add("links", showtimeLinks);

               return showtimeDictionary;
           });
            if(requestParameters.IncludeMetadata)
            {
                var showtimesWithMetadata = new ExpandoObject();
                ((IDictionary<string, object>)showtimesWithMetadata).Add("Metadata", paginationMetadataWithLinks);
                ((IDictionary<string, object>)showtimesWithMetadata).Add("showtimes", shapedShowtimesWithLinks);
                ((IDictionary<string, object>)showtimesWithMetadata).Add("links", links);
                return showtimesWithMetadata;
            }
            else
            {
                var linkedCollection = new ExpandoObject();
                ((IDictionary<string, object>)linkedCollection).Add("showtimes", shapedShowtimesWithLinks);
                ((IDictionary<string, object>)linkedCollection).Add("links", links);
                return linkedCollection;
            }
        }

        private IEnumerable<LinkDto> CreateLinksForShowtimes(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if(string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(_urlHelper.Link("GetShowtime", new { showtimeId = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(_urlHelper.Link("GetShowtime", new { showtimeId = id, fields }), "self", "GET"));
            }
                links.Add(new LinkDto(_urlHelper.Link("DeleteShowtime", new { showtimeId = id }), "delete_showtime", "DELETE"));
            {
                links.Add(new LinkDto(_urlHelper.Link("UpdateShowtime", new { showtimeId = id }), "update_showtime", "PUT"));
            }
                links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateShowtime", new { showtimeId = id }), "partially_update_showtime", "PATCH"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForShowtimes(RequestParameters requestParameters, bool hasNext, bool hasPrevious)
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
                    return _urlHelper.Link("GetShowtimes", new
                    {
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber - 1,
                        pageSize = requestParameters.PageSize

                    });
                case PageType.NextPage:
                    return _urlHelper.Link("GetShowtimes", new
                    {
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber + 1,
                        pageSize = requestParameters.PageSize
                    });
                default:
                    return _urlHelper.Link("GetShowtimes", new
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
