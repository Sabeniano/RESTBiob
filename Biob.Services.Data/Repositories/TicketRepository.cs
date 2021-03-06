﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Biob.Data.Data;
using Biob.Data.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Biob.Services.Data.Helpers;
using Biob.Services.Web.PropertyMapping;
using Biob.Services.Data.DtoModels.TicketDtos;

namespace Biob.Services.Data.Repositories
{
    public class TicketRepository : Repository, ITicketRepository
    {
        private IPropertyMappingService _propertyMappingService;

        public TicketRepository(IPropertyMappingService propertyMappingService, BiobDataContext context) : base(context)
        {
            _propertyMappingService = propertyMappingService;
            _propertyMappingService.AddPropertyMapping<TicketDto, Ticket>(new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new PropertyMappingValue(new List<string>() { "Id" })},
                { "CustomerId", new PropertyMappingValue(new List<string>() { "CustomerId" })},
                { "ShowtimeId", new PropertyMappingValue(new List<string>() { "ShowtimeId" })},
                { "HallseatId", new PropertyMappingValue(new List<string>() { "HallseatId" })},
                { "Reserved", new PropertyMappingValue(new List<string>() { "Reserved" })},
                { "Paid", new PropertyMappingValue(new List<string>() { "Paid" })},
                { "Price", new PropertyMappingValue(new List<string>() { "Price" })},
            });
        }
        public void AddTicket(Ticket ticketToAdd)
        {
            _context.Tickets.Add(ticketToAdd);
        }

        public void DeleteTicket(Ticket ticketToDelete)
        {
            ticketToDelete.IsDeleted = true;
            ticketToDelete.DeletedOn = DateTimeOffset.Now;
        }

        public async Task<Ticket> GetTicketAsync(Guid id)
        {
            var foundTicket =  await _context.Tickets.Where(ticket => ticket.Id == id && !ticket.IsDeleted).FirstOrDefaultAsync();
            return foundTicket;
        }

        public async Task<PagedList<Ticket>> GetAllTicketsByShowtimeIdAsync(Guid showtimeId, string orderBy, string searchQuery, int pageNumber, int pageSize)
        {
                                                            
            var collectionsBeforePaging =_context.Tickets
                                    .Where(ticket => !ticket.IsDeleted && ticket.ShowtimeId == showtimeId)
                                    .Applysort(orderBy, _propertyMappingService.GetPropertyMapping<TicketDto, Ticket>());

            var listToPage = await collectionsBeforePaging.ToListAsync();
            return PagedList<Ticket>.Create(listToPage, pageNumber, pageSize);
        }
        public void UpdateTicket(Ticket ticketToUpdate)
        {
            _context.Tickets.Update(ticketToUpdate);
        }
    }
}
