using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Biob.Data.Data;
using Biob.Data.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Biob.Services.Web.PropertyMapping;
using Biob.Services.Data.Helpers;
using Biob.Services.Data.DtoModels.ShowtimeDtos;

namespace Biob.Services.Data.Repositories
{
    public class ShowtimeRepository : Repository, IShowtimeRepository
    {
        private IPropertyMappingService _propertyMappingService;

        public ShowtimeRepository(IPropertyMappingService propertyMappingService, BiobDataContext context) : base(context)
        {
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

        public void AddShowtime(Guid movieId, Showtime showtimeToAdd)
        {
            if (showtimeToAdd.Id == Guid.Empty)
            {
                showtimeToAdd.Id = Guid.NewGuid();
            }
            if (showtimeToAdd.MovieId == Guid.Empty)
            {
                showtimeToAdd.MovieId = movieId;
            }
            _context.Showtimes.Add(showtimeToAdd);
        }

        public void DeleteShowtime(Showtime showtimeToDelete)
        {
            showtimeToDelete.IsDeleted = true;
            showtimeToDelete.DeletedOn = DateTimeOffset.Now;
        }

        public async Task<PagedList<Showtime>> GetAllShowtimesAsync(Guid movieId, string orderBy, int pageNumber, int pageSize)
        {
            var collectionBeforePaging = _context.Showtimes.Where(showtime => showtime.MovieId == movieId).Applysort(orderBy, _propertyMappingService.GetPropertyMapping<ShowtimeDto, Showtime>());
            var listToPage = await collectionBeforePaging.ToListAsync();
            return PagedList<Showtime>.Create(listToPage, pageNumber, pageSize);
        }

        public async Task<Showtime> GetShowtimeAsync(Guid showtimeId)
        {
            var foundShowtime = await _context.Showtimes.Where(showtime => showtime.Id == showtimeId).FirstOrDefaultAsync();
            if (foundShowtime.IsDeleted)
            {
                foundShowtime = null;
            }
            return foundShowtime;
        }

        public async Task<Showtime> GetShowtimeAsync( Guid showtimeId, Guid movieId)
        {
            var foundShowtime = await _context.Showtimes.Where(showtime => showtime.MovieId == movieId && showtime.Id == showtimeId).FirstOrDefaultAsync();
            if (foundShowtime.IsDeleted)
            {
                foundShowtime = null;
            }
            return foundShowtime;
        }

        public void UpdateShowtime(Showtime showtimeToUpdate)
        {
            _context.Showtimes.Update(showtimeToUpdate);
        }
    }
}
