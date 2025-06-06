using Microsoft.EntityFrameworkCore;
using ConcertTicketAPI.Data;
using ConcertTicketAPI.Models;
using ConcertTicketAPI.DTOs;

namespace ConcertTicketAPI.Services
{
    public class EventService : IEventService
    {
        private readonly ConcertTicketContext _context;

        public EventService(ConcertTicketContext context)
        {
            _context = context;
        }

        public async Task<EventResponse> CreateEventAsync(CreateEventRequest request)
        {
            var eventEntity = new Event
            {
                Title = request.Title,
                Description = request.Description,
                EventDate = request.EventDate,
                Venue = request.Venue,
                VenueAddress = request.VenueAddress,
                TotalCapacity = request.TotalCapacity,
                AvailableCapacity = request.TotalCapacity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            // Add ticket types
            foreach (var ticketTypeRequest in request.TicketTypes)
            {
                var ticketType = new TicketType
                {
                    EventId = eventEntity.Id,
                    Name = ticketTypeRequest.Name,
                    Description = ticketTypeRequest.Description,
                    Price = ticketTypeRequest.Price,
                    Capacity = ticketTypeRequest.Capacity,
                    AvailableCount = ticketTypeRequest.Capacity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.TicketTypes.Add(ticketType);
            }

            await _context.SaveChangesAsync();

            return await GetEventResponseAsync(eventEntity.Id);
        }

        public async Task<EventResponse?> GetEventAsync(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == id);

            return eventEntity == null ? null : MapToEventResponse(eventEntity);
        }

        public async Task<IEnumerable<EventResponse>> GetEventsAsync(bool includeInactive = false)
        {
            var query = _context.Events.Include(e => e.TicketTypes).AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();
            return events.Select(MapToEventResponse);
        }

        public async Task<EventResponse?> UpdateEventAsync(int id, UpdateEventRequest request)
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null) return null;

            if (!string.IsNullOrEmpty(request.Title))
                eventEntity.Title = request.Title;
            
            if (!string.IsNullOrEmpty(request.Description))
                eventEntity.Description = request.Description;
            
            if (request.EventDate.HasValue)
                eventEntity.EventDate = request.EventDate.Value;
            
            if (!string.IsNullOrEmpty(request.Venue))
                eventEntity.Venue = request.Venue;
            
            if (!string.IsNullOrEmpty(request.VenueAddress))
                eventEntity.VenueAddress = request.VenueAddress;
            
            if (request.TotalCapacity.HasValue)
            {
                var capacityDifference = request.TotalCapacity.Value - eventEntity.TotalCapacity;
                eventEntity.TotalCapacity = request.TotalCapacity.Value;
                eventEntity.AvailableCapacity += capacityDifference;
            }
            
            if (request.IsActive.HasValue)
                eventEntity.IsActive = request.IsActive.Value;

            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEventResponseAsync(id);
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null) return false;

            // Check if there are any sold tickets
            var hasSoldTickets = await _context.Tickets
                .AnyAsync(t => t.EventId == id && t.Status == TicketStatus.Purchased);

            if (hasSoldTickets)
            {
                // Soft delete - mark as inactive instead of deleting
                eventEntity.IsActive = false;
                eventEntity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            // Hard delete if no sold tickets
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TicketAvailabilityResponse?> GetEventAvailabilityAsync(int eventId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.IsActive);

            if (eventEntity == null) return null;

            var ticketTypeAvailabilities = eventEntity.TicketTypes
                .Where(tt => tt.IsActive)
                .Select(tt => new TicketTypeAvailability
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    Description = tt.Description,
                    Price = tt.Price,
                    Capacity = tt.Capacity,
                    AvailableCount = tt.AvailableCount,
                    ReservedCount = tt.Tickets.Count(t => t.Status == TicketStatus.Reserved),
                    SoldCount = tt.Tickets.Count(t => t.Status == TicketStatus.Purchased)
                }).ToList();

            return new TicketAvailabilityResponse
            {
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                EventDate = eventEntity.EventDate,
                Venue = eventEntity.Venue,
                TotalCapacity = eventEntity.TotalCapacity,
                AvailableCapacity = eventEntity.AvailableCapacity,
                TicketTypes = ticketTypeAvailabilities
            };
        }



        public async Task<bool> EventExistsAsync(int id)
        {
            return await _context.Events.AnyAsync(e => e.Id == id);
        }

        private async Task<EventResponse> GetEventResponseAsync(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstAsync(e => e.Id == id);

            return MapToEventResponse(eventEntity);
        }

        private static EventResponse MapToEventResponse(Event eventEntity)
        {
            return new EventResponse
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                EventDate = eventEntity.EventDate,
                Venue = eventEntity.Venue,
                VenueAddress = eventEntity.VenueAddress,
                TotalCapacity = eventEntity.TotalCapacity,
                AvailableCapacity = eventEntity.AvailableCapacity,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt,
                IsActive = eventEntity.IsActive,
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeResponse
                {
                    Id = tt.Id,
                    EventId = tt.EventId,
                    Name = tt.Name,
                    Description = tt.Description,
                    Price = tt.Price,
                    Capacity = tt.Capacity,
                    AvailableCount = tt.AvailableCount,
                    IsActive = tt.IsActive
                }).ToList()
            };
        }
    }
} 