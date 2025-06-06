using ConcertTicketAPI.DTOs;

namespace ConcertTicketAPI.Services
{
    public interface IEventService
    {
        Task<EventResponse> CreateEventAsync(CreateEventRequest request);
        Task<EventResponse?> GetEventAsync(int id);
        Task<IEnumerable<EventResponse>> GetEventsAsync(bool includeInactive = false);
        Task<EventResponse?> UpdateEventAsync(int id, UpdateEventRequest request);
        Task<bool> DeleteEventAsync(int id);
        Task<TicketAvailabilityResponse?> GetEventAvailabilityAsync(int eventId);

        Task<bool> EventExistsAsync(int id);
    }
} 