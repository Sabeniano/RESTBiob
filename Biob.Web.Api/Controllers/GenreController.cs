using AutoMapper;
using Biob.Data.Models;
using Biob.Services.Data.DtoModels.GenreDtos;
using Biob.Services.Data.Helpers;
using Biob.Services.Data.Repositories;
using Biob.Web.Api.Filters;
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
    [Route("api/v1/genres")]
    [ApiController]
    public class GenreController : ControllerBase
    {
        private readonly IGenreRepository _genreRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<GenreController> _logger;

        public GenreController(IGenreRepository genreRepository, IUrlHelper urlHelper, ILogger<GenreController> logger)
        {
            _genreRepository = genreRepository;
            _urlHelper = urlHelper;
            _logger = logger;
        }

        [SwaggerOperation(
            Summary = "Retrieve every genres",
            Description = "Retrieves every genres in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every genre", typeof(GenreDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet]
        public async Task<IActionResult> GetAllGenresAsync(
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {
            var genres = await _genreRepository.GetAllGenresAsync();

            var mappedGenres = Mapper.Map<IEnumerable<GenreDto>>(genres);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                return Ok(CreateLinksForGenres(mappedGenres));
            }

            return Ok(mappedGenres);
        }

        [SwaggerOperation(
            Summary = "Retrieve one genre by ID",
            Description = "Retrieves genre in the database by id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a genre", typeof(GenreDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{genreId}", Name = "GetGenre")]
        [GuidCheckActionFilter(new string[] { "genreId" })]
        public async Task<IActionResult> GetGenreByIdAsync(
            [FromRoute, SwaggerParameter(Description = "the ID to find genre by", Required = true)] Guid genreId, 
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {
            var genreFound = await _genreRepository.GetGenreByIdAsync(genreId);

            if (genreFound == null)
            {
                return NotFound();
            }

            var mappedGenre = Mapper.Map<GenreDto>(genreFound);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForGenre(genreId);

                var linkedGenre = mappedGenre.ShapeData(null) as IDictionary<string, object>;

                linkedGenre.Add("links", links);

                return Ok(linkedGenre);
            }
            return Ok(mappedGenre);
        }

        [SwaggerOperation(
            Summary = "Create a genre",
            Description = "Creates a genre in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a genre", typeof(GenreDto))]
        [HttpPost]
        public async Task<IActionResult> CreateGenreAsync(
            [FromBody, SwaggerParameter(Description = "Genre to create", Required = true)]GenreToCreateDto genreToCreate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request between json or json+hateoas")] string mediaType)
        {
            var genreToAdd = Mapper.Map<Genre>(genreToCreate);
            _genreRepository.AddGenre(genreToAdd);

            if (!await _genreRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a genre failed");
            }

            // mapped created genre back to genreDto to remove unnecessary information
            var genreToReturn = Mapper.Map<GenreDto>(genreToAdd);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForGenre(genreToReturn.Id);

                var linkedGenre = genreToReturn.ShapeData(null) as IDictionary<string, object>;

                linkedGenre.Add("links", links);

                return CreatedAtRoute("GetGenre", new { genreId = genreToReturn.Id }, linkedGenre);
            }
        
            return CreatedAtRoute("GetGenre", new { genreId = genreToReturn.Id }, genreToReturn);
        }

        [SwaggerOperation(
            Summary = "Partially update a genre",
            Description = "Partially updates a genre in the database",
            Consumes = new string[] { "application/json-patch+json" },
            Produces = new string[] { "application/json" })]
        [SwaggerResponse(200, "Successfully partially updated a genre", typeof(GenreDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{genreId}", Name = "PartiallyUpdateGenre")]
        [GuidCheckActionFilter(new string[] { "genreId" })]
        public async Task<IActionResult> PartiallyUpdateGenreAsync(
            [FromRoute, SwaggerParameter(Description = "Id of genre to update", Required = true)] Guid genreId,
            [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<GenreToUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var genreFromDb = await _genreRepository.GetGenreByIdAsync(genreId);

            var genreToPatch = Mapper.Map<GenreToUpdateDto>(genreFromDb);

            patchDoc.ApplyTo(genreToPatch, ModelState);

            Mapper.Map(genreToPatch, genreFromDb);

            _genreRepository.UpdateGenre(genreFromDb);

            if (!await _genreRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating movie: {genreId} failed on save");
            }

            return NoContent();

        }

        [SwaggerOperation(
            Summary = "Update a genre",
            Description = "Updates a genre in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json" })]
        [SwaggerResponse(200, "Successfully updated a genre", typeof(GenreDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{genreId}", Name = "UpdateGenre")]
        [GuidCheckActionFilter(new string[] { "genreId" })]
        public async Task<IActionResult> UpdateGenreAsync(
            [FromRoute, SwaggerParameter(Description = "The id of genre to update", Required = true)] Guid genreId, 
            [FromBody, SwaggerParameter(Description = "Genre to update", Required = true)] GenreToUpdateDto genreToUpdate)
        {
            var genreFromDb = await _genreRepository.GetGenreByIdAsync(genreId);

            Mapper.Map(genreToUpdate, genreFromDb);

            _genreRepository.UpdateGenre(genreFromDb);

            if (!await _genreRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating movie: {genreId} failed on save");
            }

            return NoContent();

        }

        [SwaggerOperation(
            Summary = "Hard deletes a genre",
            Description = "Hard deletes a genre in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json" })]
        [SwaggerResponse(200, "Successfully deleted a genre", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{genreId}", Name = "DeleteGenre")]
        [GuidCheckActionFilter(new string[] { "genreId" })]
        public async Task<IActionResult> DeleteGenreAsync(
            [FromRoute, SwaggerParameter(Description = "Id of genre to delete", Required = true)] Guid genreId)
        {
            var genreToDelete = await _genreRepository.GetGenreByIdAsync(genreId);

            if (genreToDelete == null)
            {
                NotFound();
            }

            _genreRepository.DeleteGenre(genreToDelete);

            if (!await _genreRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting genre: {genreId} failed on save");
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
        public IActionResult GetGenresOptions()
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
        [HttpOptions("{genreId}")]
        public IActionResult GetGenreOptions()
        {
            Response.Headers.Add("Allow", "GET,PATCH,PUT,OPTIONS,DELETE");
            return Ok();
        }

        private IEnumerable<LinkDto> CreateLinksForGenre(Guid id)
        {
            var links = new List<LinkDto>
            {
                new LinkDto(_urlHelper.Link("GetGenre", new { genreId = id }), "self", "GET"),
                new LinkDto(_urlHelper.Link("UpdateGenre", new { genreId = id }), "update_genre", "PUT"),
                new LinkDto(_urlHelper.Link("PartiallyUpdateGenre", new { genreId = id }), "partially_update_genre", "PATCH"),
                new LinkDto(_urlHelper.Link("DeleteGenre", new { genreId = id }), "delete_genre", "DELETE")
            };

            return links;
        }

        private ExpandoObject CreateLinksForGenres(IEnumerable<GenreDto> genreList)
        {
            var shapedGenres = genreList.ShapeData(null);
            var genresWithLinks = shapedGenres.Select(genre =>
            {
                var genreDictionary = genre as IDictionary<string, object>;
                var genreLinks = CreateLinksForGenre((Guid)genreDictionary["Id"]);

                genreDictionary.Add("links", genreLinks);

                return genreDictionary;
            });
            var linkedCollection = new ExpandoObject();
            ((IDictionary<string, object>)linkedCollection).Add("genres", genresWithLinks);

            return linkedCollection;
        }
    }
}
