using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Biob.Services.Data.Repositories;
using AutoMapper;
using Biob.Data.Models;
using Biob.Web.Api.Helpers;
using Microsoft.AspNetCore.JsonPatch;
using Biob.Web.Api.Filters;
using Biob.Services.Web.PropertyMapping;
using System.Collections.Generic;
using Biob.Services.Data.Helpers;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using System.Linq;
using Biob.Services.Data.DtoModels.MovieDtos;
using Swashbuckle.AspNetCore.Annotations;

namespace Biob.Web.Api.Controllers
{
    [Route("/api/v1/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMovieGenreRepository _movieGenreRepository;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<MovieController> _logger;

        public MovieController(IMovieRepository movieRepository, IMovieGenreRepository movieGenreRepository,
                               IPropertyMappingService propertyMappingService, ITypeHelperService typeHelperService,
                               IUrlHelper urlHelper, ILogger<MovieController> logger)
        {
            _movieRepository = movieRepository;
            _movieGenreRepository = movieGenreRepository;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
            _urlHelper = urlHelper;
            _logger = logger;
            _propertyMappingService.AddPropertyMapping<MovieDto, Movie>(new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new PropertyMappingValue(new List<string>() { "Id" })},
                { "Title", new PropertyMappingValue(new List<string>() { "Title" })},
                { "Description", new PropertyMappingValue(new List<string>() { "Description" })},
                { "Length", new PropertyMappingValue(new List<string>() { "LengthInSeconds" })},
                { "Poster", new PropertyMappingValue(new List<string>() { "Poster" })},
                { "Producer", new PropertyMappingValue(new List<string>() { "Producer" })},
                { "Actors", new PropertyMappingValue(new List<string>() { "Actors" })},
                { "Genre", new PropertyMappingValue(new List<string>() { "Genre" })},
                { "Released", new PropertyMappingValue(new List<string>() { "Released" })},
                { "ThreeDee", new PropertyMappingValue(new List<string>() { "ThreeDee" })},
                { "AgeRestriction", new PropertyMappingValue(new List<string>() { "AgeRestriction" })},
            });
        }


        [SwaggerOperation(
            Summary = "Retrieve every movie",
            Description = "Retrieves every movie in the database",
            Consumes = new string[] {},
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved every movie", typeof(MovieDto[]))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet(Name = "GetMovies")]
        public async Task<IActionResult> GetAllMoviesAsync(
            [FromQuery] RequestParameters requestParameters,
            [FromHeader(Name = "Accept")] string mediaType)
        {

            if (string.IsNullOrWhiteSpace(requestParameters.OrderBy))
            {
                requestParameters.OrderBy = "Title";
            }

            if (!_propertyMappingService.ValidMappingExistsFor<MovieDto, Movie>(requestParameters.Fields))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<MovieDto>(requestParameters.Fields))
            {
                return BadRequest();
            }

            var moviesPagedList = await _movieRepository.GetAllMoviesAsync(requestParameters.OrderBy,
                                                                    requestParameters.SearchQuery,
                                                                    requestParameters.PageNumber, requestParameters.PageSize);

            var movies = Mapper.Map<IEnumerable<MovieDto>>(moviesPagedList);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {  
                return Ok(CreateHateoasResponse(moviesPagedList, requestParameters));
            }
            else
            {
                var previousPageLink = moviesPagedList.HasPrevious ? CreateUrlForResource(requestParameters, PageType.PreviousPage) : null;
                var nextPageLink = moviesPagedList.HasNext ? CreateUrlForResource(requestParameters, PageType.NextPage) : null;
                var paginationMetadata = new PaginationMetadata()
                {
                    TotalCount = moviesPagedList.TotalCount,
                    PageSize = moviesPagedList.PageSize,
                    CurrentPage = moviesPagedList.CurrentPage,
                    TotalPages = moviesPagedList.TotalPages,
                    PreviousPageLink = previousPageLink,
                    NextPageLink = nextPageLink
                };

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                if (requestParameters.IncludeMetadata)
                {
                    var shapedMovies = movies.ShapeData(requestParameters.Fields);
                    var moviesWithMetadata = new EntityWithPaginationMetadataDto<ExpandoObject>(paginationMetadata, shapedMovies);
                    return Ok(moviesWithMetadata);
                }

                return Ok(movies.ShapeData(requestParameters.Fields));
            }
        }

        [SwaggerOperation(
            Summary = "Retrieve one movie by ID",
            Description = "Retrieves movie in the database by id",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully retrieved a movie", typeof(MovieDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpGet("{movieId}", Name = "GetMovie")]
        [GuidCheckActionFilter(new string[] { "movieId" })]
        public async Task<IActionResult> GetOneMovieAsync(
            [FromRoute, SwaggerParameter(Description = "the ID to find movie by", Required = true)] Guid movieId,
            [FromQuery, SwaggerParameter(Description = "fields requested for data shaping", Required = false)] string fields,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            if (!_typeHelperService.TypeHasProperties<MovieDto>(fields))
            {
                return BadRequest();
            }

            var foundMovie = await _movieRepository.GetMovieAsync(movieId);

            var movie = Mapper.Map<MovieDto>(foundMovie);

            if (foundMovie == null)
            {
                return NotFound();
            }

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForMovies(movieId, fields);

                var linkedMovie = movie.ShapeData(fields) as IDictionary<string, object>;
                linkedMovie.Add("links", links);
                return Ok(linkedMovie);
            }
            else
            {
                return Ok(movie.ShapeData(fields));
            }
        }


        [SwaggerOperation(
            Summary = "Create a movie",
            Description = "creates a movie in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully created a movie", typeof(MovieDto))]
        [HttpPost(Name = "CreateMovie")]
        public async Task<IActionResult> CreateMovieAsync(
            [FromBody, SwaggerParameter(Description = "Movie to create", Required = true)] MovieToCreateDto movieToCreate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            var movieToAdd = Mapper.Map<Movie>(movieToCreate);
            movieToAdd.Id = Guid.NewGuid();

            _movieRepository.AddMovie(movieToAdd);

            if (!await _movieRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while creating a movie failed");
            }

            // if any genre is added to movie, create many-to-many relationship for each genre
            if (movieToCreate.GenreIds != null && movieToCreate.GenreIds.Count > 0)
            {
                for (int i = 0; i < movieToCreate.GenreIds.Count; i++)
                {
                    // create moviegenre object with genre id and movie id from movieToCreate object
                    MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieToAdd.Id, GenreId = movieToCreate.GenreIds[i] };
                    movieGenreToAdd.Id = Guid.NewGuid();
                    _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                }
                // save changes to database after all many-to-many relationships has been created
                if (!await _movieGenreRepository.SaveChangesAsync())
                {
                    _logger.LogError("Saving changes to database while creating a moviegenre failed");
                }

                

            }

            var foundMovie = await _movieRepository.GetMovieAsync(movieToAdd.Id);

            var movieDto = Mapper.Map<MovieDto>(foundMovie);

            if (mediaType == "application/vnd.biob.json+hateoas")
            {
                var links = CreateLinksForMovies(movieDto.Id, null);

                var linkedMovie = movieDto.ShapeData(null) as IDictionary<string, object>;

                linkedMovie.Add("links", links);

                return CreatedAtRoute("GetMovie", new { movieId = movieDto.Id }, linkedMovie);
            }
            else
            {
                return CreatedAtRoute("GetMovie", new { movieId = movieDto.Id }, movieDto);
            }
               
        }

        [SwaggerOperation(
            Summary = "Update a movie",
            Description = "Updates a movie in the database",
            Consumes = new string[] { "application/json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully updated a movie", typeof(MovieDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPut("{movieId}",Name = "UpdateMovie")]
        [GuidCheckActionFilter(new string[] { "movieId" })]
        public async Task<IActionResult> UpdateMovieAsync(
            [FromRoute, SwaggerParameter(Description = "Id of movie to update", Required = true)] Guid movieId,
            [FromBody, SwaggerParameter(Description = "Movie to update", Required = true)] MovieToUpdateDto movieToUpdate,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {

            var movieFromDb = await _movieRepository.GetMovieAsync(movieId);

            //  upserting if movie does not already exist
            if (movieFromDb == null)
            {
                var movieEntity = Mapper.Map<Movie>(movieToUpdate);
                movieEntity.Id = movieId;
                _movieRepository.AddMovie(movieEntity);

                if (!await _movieRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting movie: {movieId} failed on save");
                }

                // if any genre is added to movie, create many-to-many relationship for each genre
                if (movieToUpdate.GenreIds.Count > 0)
                {
                    for (int i = 0; i < movieToUpdate.GenreIds.Count; i++)
                    {
                        // create moviegenre object with genre id and movie id from movieToCreate object
                        MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = movieToUpdate.GenreIds[i] };
                        movieGenreToAdd.Id = Guid.NewGuid();
                        _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                    }

                    // save changes to database after all many-to-many relationships has been created if any exist
                    if (!await _movieGenreRepository.SaveChangesAsync())
                    {
                        _logger.LogError("Saving changes to database while deleting a moviegenre failed");
                    }
                }

                var movieToReturn = Mapper.Map<MovieDto>(movieEntity);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForMovies(movieToReturn.Id, null);

                    var linkedMovie = movieToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedMovie.Add("links", links);

                    return CreatedAtRoute("GetMovie", new { movieId = movieToReturn.Id }, linkedMovie);
                }
                else
                {
                    return CreatedAtRoute("GetMovie", new { movieId = movieToReturn.Id }, movieToReturn);
                }
            }

            Mapper.Map(movieToUpdate, movieFromDb);

            _movieRepository.UpdateMovie(movieFromDb);

            if (!await _movieRepository.SaveChangesAsync())
            {
                _logger.LogError($"Updating movie: {movieId} failed on save");
            }

            // easy solution delete all moviegenres with id, add new ids
            var moviegenresExist = await _movieGenreRepository.GetAllMovieGenresByMovieIdAsync(movieId);

            // delete all existing many-to-many genre relationships for movie
            foreach (var moviegenre in moviegenresExist)
            {
                _movieGenreRepository.DeleteMovieGenre(moviegenre);
            }

            // if any genre is added to movie, create many-to-many relationship for each genre
            if (movieToUpdate.GenreIds.Count > 0)
            {
                for (int i = 0; i < movieToUpdate.GenreIds.Count; i++)
                {
                    // create moviegenre object with genre id and movie id from movieToCreate object
                    MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = movieToUpdate.GenreIds[i] };
                    movieGenreToAdd.Id = Guid.NewGuid();
                    _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                }
            }

            // save changes to database after all many-to-many relationships has been deleted and recreated if any exist
            if (!await _movieGenreRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while deleting a moviegenre failed");
            }


            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Partially update a movie",
            Description = "Partially updates a movie in the database",
            Consumes = new string[] { "application/json-patch+json" },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully partially updated a movie", typeof(MovieDto))]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpPatch("{movieId}", Name = "PartiallyUpdateMovie")]
        [GuidCheckActionFilter(new string[] { "movieId" })]
        public async Task<IActionResult> PartiuallyUpdateMovieAsync(
            [FromRoute, SwaggerParameter(Description = "Id of movie to update", Required = true)] Guid movieId,
            [FromBody, SwaggerParameter(Description = "Jsonpatch operation document to update", Required = true)] JsonPatchDocument<MovieToUpdateDto> patchDoc,
            [FromHeader(Name = "Accept"), SwaggerParameter(Description = "media type to request betwen json or json+hateoas")] string mediaType)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var movieFromDb = await _movieRepository.GetMovieAsync(movieId);

            //  upserting if movie does not already exist
            //  TODO:   research if upserting is neccesary in patching
            if (movieFromDb == null)
            {
                var movieToCreate = new MovieToUpdateDto();

                patchDoc.ApplyTo(movieToCreate, ModelState);

                if (!ModelState.IsValid)
                {
                    new ProccessingEntityObjectResultErrors(ModelState);
                }

                var movieToAddToDb = Mapper.Map<Movie>(movieToCreate);
                movieToAddToDb.Id = movieId;

                _movieRepository.AddMovie(movieToAddToDb);

                if (!await _movieRepository.SaveChangesAsync())
                {
                    _logger.LogError($"Upserting movie: {movieId} failed on save");
                }

                // loop on foreach Operations => if op.path == "GenreIds" => loop on value which contains list/array? of genreIds

                foreach (var op in patchDoc.Operations)
                {
                    if (op.path.ToLower() == "genreids")
                    {
                        //movieToPatch.GenreIds = (List<Guid>)op.value;
                        var genreIds = op.value.ToString();

                        // TODO make list guid instead of string so you dont have to parse on each use. What if only one, still split?
                        // TODO EXTENDED check if lenght is > 0 before all this, also check if valid Guids
                        var splitGenreIds = genreIds.Split(",");

                        switch (op.op)
                        {
                            // check if genre already exists on movie before creating
                            case "add":
                                {
                                    for (int i = 0; i < splitGenreIds.Length; i++)
                                    {
                                        //create moviegenre object with genre id and movie id from movieToCreate object
                                        MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = Guid.Parse(splitGenreIds[i]) };
                                        movieGenreToAdd.Id = Guid.NewGuid();
                                        _movieGenreRepository.AddMovieGenre(movieGenreToAdd);   
                                    }
                                    break;
                                }
                            case "replace":
                                {
                                    // easy solution delete all moviegenres with id, add new ids
                                    var moviegenresExist = await _movieGenreRepository.GetAllMovieGenresByMovieIdAsync(movieId);

                                    // delete all existing many-to-many genre relationships for movie
                                    foreach (var moviegenre in moviegenresExist)
                                    {
                                        _movieGenreRepository.DeleteMovieGenre(moviegenre);
                                    }

                                    // if any genre is added to movie, create many-to-many relationship for each genre

                                    for (int i = 0; i < splitGenreIds.Length; i++)
                                    {
                                        // create moviegenre object with genre id and movie id from movieToCreate object
                                        MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = Guid.Parse(splitGenreIds[i]) };
                                        movieGenreToAdd.Id = Guid.NewGuid();
                                        _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                                    }

                                    break;
                                }

                            default:
                                // tell user operation not allowed?
                                break;
                        }

                    }
                }

                // save changes to database after all operations
                // check if state is modified? this will always get run
                if (!await _movieGenreRepository.SaveChangesAsync())
                {
                    _logger.LogError("Saving changes to database while dealing with a moviegenre failed");
                }

                var movieToReturn = Mapper.Map<MovieDto>(movieToAddToDb);

                if (mediaType == "application/vnd.biob.json+hateoas")
                {
                    var links = CreateLinksForMovies(movieToReturn.Id, null);

                    var linkedMovie = movieToReturn.ShapeData(null) as IDictionary<string, object>;

                    linkedMovie.Add("links", links);

                    return CreatedAtRoute("GetMovie", new { movieId = movieToReturn.Id }, linkedMovie);
                }
                else
                {
                    return CreatedAtRoute("GetMovie", new { movieId = movieToReturn.Id }, movieToReturn);
                }
                
            }

            var movieToPatch = Mapper.Map<MovieToUpdateDto>(movieFromDb);

            patchDoc.ApplyTo(movieToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                new ProccessingEntityObjectResultErrors(ModelState);
            }

            Mapper.Map(movieToPatch, movieFromDb);

            _movieRepository.UpdateMovie(movieFromDb);

            if (!await _movieRepository.SaveChangesAsync())
            {
                _logger.LogError($"Partially updating movie: {movieId} failed on save");
            }

            // loop on foreach Operations => if op.path == "GenreIds" => loop on value which contains list/array? of genreIds

            foreach (var op in patchDoc.Operations)
            {
                if (op.path.ToLower() == "genreids")
                {
                    //movieToPatch.GenreIds = (List<Guid>)op.value;
                    var genreIds = op.value.ToString();

                    // TODO make list guid instead of string so you dont have to parse on each use. What if only one, still split?
                    // TODO EXTENDED check if lenght is > 0 before all this, also check if valid Guids
                    var splitGenreIds = genreIds.Split(",");

                    switch (op.op)
                    {
                        case "add":
                            {
                                for (int i = 0; i < splitGenreIds.Length; i++)
                                {
                                    // checking if genre already exists on movie
                                    // will fail if we use wrapper class IsDeleted checker, NOT USING ATM
                                    var moviegenreToAdd = await _movieGenreRepository.GetMovieGenreByMovieIdGenreIdAsync(movieId, Guid.Parse(splitGenreIds[i]));
                                    if (moviegenreToAdd == null)
                                    {
                                        //create moviegenre object with genre id and movie id from movieToCreate object
                                        MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = Guid.Parse(splitGenreIds[i]) };
                                        movieGenreToAdd.Id = Guid.NewGuid();
                                        _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                                    }
                                }
                                break;
                            }
                        case "replace":
                            {
                                // easy solution delete all moviegenres with id, add new ids
                                var moviegenresExist = await _movieGenreRepository.GetAllMovieGenresByMovieIdAsync(movieId);

                                // delete all existing many-to-many genre relationships for movie
                                foreach (var moviegenre in moviegenresExist)
                                {
                                    _movieGenreRepository.DeleteMovieGenre(moviegenre);
                                }

                                // if any genre is added to movie, create many-to-many relationship for each genre
                                
                                for (int i = 0; i < splitGenreIds.Length; i++)
                                {
                                    // create moviegenre object with genre id and movie id from movieToCreate object
                                    MovieGenre movieGenreToAdd = new MovieGenre { MovieId = movieId, GenreId = Guid.Parse(splitGenreIds[i]) };
                                    movieGenreToAdd.Id = Guid.NewGuid();
                                    _movieGenreRepository.AddMovieGenre(movieGenreToAdd);
                                }
                                
                                break;
                            }
                        case "remove":
                            {
                                for (int i = 0; i < splitGenreIds.Length; i++)
                                {
                                    var moviegenreToDelete = await _movieGenreRepository.GetMovieGenreByMovieIdGenreIdAsync(movieId, Guid.Parse(splitGenreIds[i]));
                                    _movieGenreRepository.DeleteMovieGenre(moviegenreToDelete);
                                }
                                break;
                            }

                        default:
                            // tell user operation not allowed?
                            break;
                    }

                }
            }

            // save changes to database after all operations
            // check if state is modified? this will always get run
            if (!await _movieGenreRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while dealing with a moviegenre failed");
            }

            return NoContent();
        }

        [SwaggerOperation(
            Summary = "Soft deletes a movie",
            Description = "Soft deletes a movie in the database",
            Consumes = new string[] { },
            Produces = new string[] { "application/json", "application/vnd.biob.json+hateoas" })]
        [SwaggerResponse(200, "Successfully deleted a movie", null)]
        [SwaggerResponse(400, "Request data is invalid", null)]
        [HttpDelete("{movieId}", Name = "DeleteMovie")]
        [GuidCheckActionFilter(new string[] { "movieId" })]
        public async Task<IActionResult> DeleteMovieAsync([FromRoute, SwaggerParameter(Description = "Id of movie to delete", Required = true)] Guid movieId)
        {
            var movieFromDb = await _movieRepository.GetMovieAsync(movieId);

            if (movieFromDb == null)
            {
                return NotFound();
            }

            _movieRepository.DeleteMovie(movieFromDb);

            if (!await _movieRepository.SaveChangesAsync())
            {
                _logger.LogError($"Deleting movie: {movieId} failed on save");
            }
            // IF its a REAL delete on movie, we need to delete many-to-many relation BEFORE movie

            // easy solution delete all moviegenres with id, add new ids
            var moviegenresExist = await _movieGenreRepository.GetAllMovieGenresByMovieIdAsync(movieId);

            // delete all existing many-to-many genre relationships for movie
            foreach (var moviegenre in moviegenresExist)
            {
                _movieGenreRepository.DeleteMovieGenre(moviegenre);
            }

            if (!await _movieGenreRepository.SaveChangesAsync())
            {
                _logger.LogError("Saving changes to database while deleting a moviegenre failed");
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
        public IActionResult GetMoviesOptions()
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
        public IActionResult GetMovieOptions()
        {
            Response.Headers.Add("Allow", "GET,PATCH,PUT,OPTIONS,DELETE");
            return Ok();
        }

        private ExpandoObject CreateHateoasResponse(PagedList<Movie> moviesPagedList, RequestParameters requestParameters)
        {

            var movies = Mapper.Map<IEnumerable<MovieDto>>(moviesPagedList);

            var paginationMetadataWithLinks = new
            {
                moviesPagedList.TotalCount,
                moviesPagedList.PageSize,
                moviesPagedList.CurrentPage,
                moviesPagedList.TotalPages
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadataWithLinks));

            var links = CreateLinksForMovies(requestParameters, moviesPagedList.HasNext, moviesPagedList.HasPrevious);

            

            var shapedMovies = movies.ShapeData(requestParameters.Fields);

            var shapedMoviesWithLinks = shapedMovies.Select(movie =>
            {
                var movieDictionary = movie as IDictionary<string, object>;
                var movieLinks = CreateLinksForMovies((Guid)movieDictionary["Id"], requestParameters.Fields);

                movieDictionary.Add("links", movieLinks);

                return movieDictionary;
            });
            if (requestParameters.IncludeMetadata)
            {
                var moviesWithMetadata = new ExpandoObject();
                ((IDictionary<string, object>)moviesWithMetadata).Add("Metadata", paginationMetadataWithLinks);
                ((IDictionary<string, object>)moviesWithMetadata).Add("movies", shapedMoviesWithLinks);
                ((IDictionary<string, object>)moviesWithMetadata).Add("links", links);
                return moviesWithMetadata;
            }
            else
            {
                var linkedCollection = new ExpandoObject();
                ((IDictionary<string, object>)linkedCollection).Add("movies", shapedMoviesWithLinks);
                ((IDictionary<string, object>)linkedCollection).Add("links", links);
                return linkedCollection;
            }
            
        }

        private IEnumerable<LinkDto> CreateLinksForMovies(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(_urlHelper.Link("GetMovie", new { movieId = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(_urlHelper.Link("GetMovie", new { movieId = id, fields }), "self", "GET"));
            }
            links.Add(
                new LinkDto(_urlHelper.Link("DeleteMovie", new { movieId = id }), "delete_movie", "DELETE")
                );
            links.Add(
                new LinkDto(_urlHelper.Link("UpdateMovie", new { movieId = id }), "update_movie", "PUT")
                );
            links.Add(
                new LinkDto(_urlHelper.Link("PartiallyUpdateMovie", new { movieId = id }), "partially_update_movie", "PATCH")
                );
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForMovies(RequestParameters requestParameters, bool hasNext, bool hasPRevious)
        {
            var links = new List<LinkDto>
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.Current), "self", "GET")
            };

            if (hasNext)
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.NextPage), "self", "GET");
            }

            if (hasPRevious)
            {
                new LinkDto(CreateUrlForResource(requestParameters, PageType.PreviousPage), "self", "GET");
            }

            return links;

        }


        //  TODO: consider making this a helper method instead, and reuseable
        private string CreateUrlForResource(RequestParameters requestParameters, PageType pageType)
        {
            switch (pageType)
            {
                case PageType.PreviousPage:
                    return _urlHelper.Link("GetMovies", new
                    {
                        fields = requestParameters.Fields,
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber - 1,
                        pageSize = requestParameters.PageSize

                    });
                case PageType.NextPage:
                    return _urlHelper.Link("GetMovies", new
                    {
                        fields = requestParameters.Fields,
                        orderBy = requestParameters.OrderBy,
                        searchQuery = requestParameters.SearchQuery,
                        pageNumber = requestParameters.PageNumber + 1,
                        pageSize = requestParameters.PageSize
                    });
                case PageType.Current:
                default:
                    return _urlHelper.Link("GetMovies", new
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
