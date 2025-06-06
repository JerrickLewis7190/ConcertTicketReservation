using ConcertTicketAPI.DTOs;
using ConcertTicketAPI.Models;

namespace ConcertTicketAPI.Services
{
    public interface ITicketService
    {
        Task<ReservationResponse> ReserveTicketsAsync(ReserveTicketRequest request);
        Task<bool> PurchaseTicketsAsync(PurchaseTicketRequest request);
        Task<bool> CancelReservationAsync(string reservationId);
        Task<TicketResponse?> GetTicketAsync(int id);
        Task<CachedReservation?> GetReservationAsync(string reservationId);

        Task<IEnumerable<TicketResponse>> GetTicketsByEventAsync(int eventId);
        Task CleanupExpiredReservationsAsync();
        Task<bool> IsTicketAvailableAsync(int ticketTypeId, int quantity);
    }
} 