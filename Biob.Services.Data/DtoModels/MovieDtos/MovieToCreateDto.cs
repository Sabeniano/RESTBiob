using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Biob.Services.Data.DtoModels.MovieDtos
{
    public class MovieToCreateDto
    {
        [Required(ErrorMessage = "Movie title is required")]
        [StringLength(100, ErrorMessage = "Movie title must not be longer than 100 characters")]
        public string Title { get; set; }
        [StringLength(5000, ErrorMessage = "Movie Description must not be longer than 5000 characters")]
        public string Description { get; set; }
        public int LengthInSeconds { get; set; }
        public string Poster { get; set; }
        [StringLength(100, ErrorMessage = "Movie producer must not be longer than 100 characters")]
        public string Producer { get; set; }
        [StringLength(255, ErrorMessage = "Movie actors must not be longer than 255 characters")]
        public string Actors { get; set; }
        public List<Guid> GenreIds { get; set; }
        public DateTimeOffset Released { get; set; }
        public bool ThreeDee { get; set; }
        [Range(1, 18, ErrorMessage = "Movie age restriction must be between 0 and 18")]
        public int AgeRestriction { get; set; }
    }
}
