using System.ComponentModel.DataAnnotations;

namespace ConcertTicketAPI.Models
{
    public class CachedReservation
    {
        public string ReservationId { get; set; } = string.Empty;
        public int EventId { get; set; }
        public int TicketTypeId { get; set; }
        public int Quantity { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal PricePerTicket { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? Notes { get; set; }
        public List<string> TicketNumbers { get; set; } = new List<string>();
        
        // Helper properties
        public decimal TotalPrice => PricePerTicket * Quantity;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;
    }
} 