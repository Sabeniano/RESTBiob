using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biob.Services.Data.Repositories;
using AutoMapper;
using Biob.Web.Api.Helpers;
using Biob.Data.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using Biob.Services.Data.Helpers;
using System.Dynamic;
using Biob.Services.Data.DtoModels.HallDtos;
using Biob.Web.Api.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace Biob.Web.Api.Controllers
{
    [Route("/api/v1/halls")]
    [ApiController]
    public class HallController : ControllerBase
    {
        private readonly IHallRepository _hallRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<HallController> _logger;

        public HallController(IHallRepository hallRepository, IUrlHelper urlHelper, ILogger<HallController> logger)
        {
            _hallRepository = hallRepository;
            _urlHelper = urlHelper;
            _logger = logger;
        }

        [SwaggerOperation(
            Summary = "Retrieve every halls",
            Description = "Retrieves every halls in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every hall", typeof(HallDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet]
        public async Task<IActionResult> GetAllHallsAsync(
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {
            var halls = await _hallRepository.GetAllHallsAsync();

            var mappedHalls = Mapper.Map<IEnumerable<HallDto>>(halls);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                return Ok(CreateLinksForHalls(mappedHalls));
            }

            return Ok(mappedHalls);
        }

        [SwaggerOperation(
            Summary = "Retrieve one hall by ID",
            Description = "Retrieves hall in the database by id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a hall", typeof(HallDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{hallId}", Name = "GetHall")]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> GetOneHallAsync(
            [FromRoute, SwaggerParameter(Description = "the ID to find hall by", Required = true)] Guid hallId, 
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {
            var foundHall = await _hallRepository.GetHallAsync(hallId);

            if (foundHall == null)
            {
                return NotFound();
            }

            var hallToReturn = Mapper.Map<HallDto>(foundHall);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForHall(hallId);

                var linkedHall = hallToReturn.ShapeData(null) as IDictionary<string, object>;

                linkedHall.Add("links", links);

                return Ok(linkedHall);
            }

            return Ok(hallToReturn);
        }

        [SwaggerOperation(
            Summary = "Create a hall",
            Description = "Creates a hall in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a hall", typeof(HallDto))]
        [HttpPost]
        public async Task<IActionResult> CreateHallAsync(
            [FromBody, SwaggerParameter(Description = "Hall to create", Required = true)] HallToCreateDto hallToCreate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {

            var hallToAdd = Mapper.Map<Hall>(hallToCreate);
            _hallRepository.AddHall(hallToAdd);

            if (!await _hallRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a hall failed");
            }

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForHall(hallToAdd.Id);

                var linkedHall = hallToAdd.ShapeData(null) as IDictionary<string, object>;

                linkedHall.Add("links", links);

                return CreatedAtRoute("GetHall", new { hallId = hallToAdd.Id }, linkedHall);
            }

            return CreatedAtRoute("GetHall", new { hallId = hallToAdd.Id }, hallToAdd);
        }

        [SwaggerOperation(
            Summary = "Update a hall",
            Description = "Updates a hall in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a hall", typeof(HallDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{hallId}", Name = "UpdateHall")]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> UpdateHallByIdAsync(
            [FromRoute, SwaggerParameter(Description = "The id of hall to update", Required = true)] Guid hallId, 
            [FromBody, SwaggerParameter(Description = "Hall to update", Required = true)] HallToUpdateDto hallToUpdate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            var hallFromDb = await _hallRepository.GetHallAsync(hallId);

            // upserting if movie does not already exist
            if (hallFromDb == null)
            {
                var hallEntity = Mapper.Map<Hall>(hallToUpdate);
                hallEntity.Id = hallId;
                _hallRepository.AddHall(hallEntity);

                if (!await _hallRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting hall: {hallId} failed on save");
                }

                var hallToReturn = Mapper.Map<HallDto>(hallEntity);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForHall(hallToReturn.Id);

                    var linkedHall = hallToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedHall.Add("links", links);

                    return CreatedAtRoute("GetHall", new { hallId = hallToReturn.Id }, linkedHall);
                }

                return CreatedAtRoute("GetHall", new { hallId = hallToReturn.Id }, hallToReturn);
            }

            Mapper.Map(hallToUpdate, hallFromDb);

            _hallRepository.UpdateHall(hallFromDb);

            if (!await _hallRepository.SaveChangesAsync())
            {
                _logger.LogError($"Updating hall: {hallId} failed on save");
            }

            return NoContent();

        }

        [SwaggerOperation(
            Summary = "Partially update a hall",
            Description = "Partially updates a hall in the database",
            Consumes = new string[] { "application/json-patch+json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully partially updated a hall", typeof(HallDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{hallId}", Name = "PartiallyUpdateHall")]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> PartiuallyUpdateHallByIdAsync(
            [FromRoute, SwaggerParameter(Description = "Id of hall to update", Required = true)] Guid hallId,
            [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<HallToUpdateDto> patchDoc,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var hallFromDb = await _hallRepository.GetHallAsync(hallId);

            //  upserting if movie does not already exist
            if (hallFromDb == null)
            {
                var hallToCreate = new HallToUpdateDto();
                patchDoc.ApplyTo(hallToCreate, ModelState);

                if (!ModelState.IsValid)
                {
                    new ProccessingEntityObjectResultErrors(ModelState);
                }

                var hallToAddToDb = Mapper.Map<Hall>(hallToCreate);
                hallToAddToDb.Id = hallId;
                _hallRepository.AddHall(hallToAddToDb);

                if (!await _hallRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting hall: {hallId} failed on save");
                }

                var hallToReturn = Mapper.Map<HallDto>(hallToAddToDb);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForHall(hallToReturn.Id);

                    var linkedHall = hallToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedHall.Add("links", links);

                    return CreatedAtRoute("GetHall", new { hallId = hallToReturn.Id }, linkedHall);
                }

                return CreatedAtRoute("GetHall", new { hallId = hallToReturn.Id }, hallToReturn);
            }

            var hallToPatch = Mapper.Map<HallToUpdateDto>(hallFromDb);

            patchDoc.ApplyTo(hallToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                new ProccessingEntityObjectResultErrors(ModelState);
            }

            Mapper.Map(hallToPatch, hallFromDb);
            _hallRepository.UpdateHall(hallFromDb);

            if (!await _hallRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating hall: {hallId} failed on save");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Hard deletes a hall",
            Description = "Hard deletes a hall in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json" })]
        [SwaggerResponse(200, "Successfully deleted a hall", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{hallId}", Name = "DeleteHall")]
        [GuidCheckActionFilter(new string[] { "hallId" })]
        public async Task<IActionResult> DeleteHallById(
            [FromRoute, SwaggerParameter(Description = "Id of hall to delete", Required = true)]Guid hallId)
        {
            var hallToDelete = await _hallRepository.GetHallAsync(hallId);

            if (hallToDelete == null)
            {
                return NotFound();
            }

            _hallRepository.DeleteHall(hallToDelete);

            if (!await _hallRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting hall: {hallId} failed on save");
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
        public IActionResult GetHallsOptions()
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
        [HttpOptions("{movieId}")]
        public IActionResult GetHallOptions()
        {
            Response.Headers.Add("Allow", "GET,PATCH,PUT,OPTIONS,DELETE");
            return Ok();
        }

        private IEnumerable<LinkDto> CreateLinksForHall(Guid id)
        {
            var links = new List<LinkDto>
            {
                new LinkDto(_urlHelper.Link("GetHall", new { hallId = id }), "self", "GET"),
                new LinkDto(_urlHelper.Link("UpdateHall", new { hallId = id }), "update_hall", "PUT"),
                new LinkDto(_urlHelper.Link("PartiallyUpdateHall", new { hallId = id }), "partially_update_hall", "PATCH"),
                new LinkDto(_urlHelper.Link("DeleteHall", new { hallId = id }), "delete_hall", "DELETE")
            };

            return links;
        }

        private ExpandoObject CreateLinksForHalls(IEnumerable<HallDto> hallList)
        {
            var shapedHalls = hallList.ShapeData(null);
            var hallWithLinks = shapedHalls.Select(hall =>
            {
                var hallDictionary = hall as IDictionary<string, object>;
                var hallLinks = CreateLinksForHall((Guid)hallDictionary["Id"]);

                hallDictionary.Add("links", hallLinks);

                return hallDictionary;
            });
            var linkedCollection = new ExpandoObject();
            ((IDictionary<string, object>)linkedCollection).Add("halls", hallWithLinks);

            return linkedCollection;
        }

    }
}
