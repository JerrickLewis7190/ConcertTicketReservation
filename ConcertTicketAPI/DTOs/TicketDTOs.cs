using System.ComponentModel.DataAnnotations;
using ConcertTicketAPI.Models;

namespace ConcertTicketAPI.DTOs
{
    public class ReserveTicketRequest
    {
        [Required]
        public int EventId { get; set; }
        
        [Required]
        public int TicketTypeId { get; set; }
        
        [Required]
        [Range(1, 10)]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class PurchaseTicketRequest
    {
        [Required]
        [StringLength(50)]
        public string ReservationId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string PaymentReference { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class TicketResponse
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int TicketTypeId { get; set; }
        public string TicketTypeName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public decimal Price { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime? PurchasedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Notes { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TicketAvailabilityResponse
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Venue { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public List<TicketTypeAvailability> TicketTypes { get; set; } = new List<TicketTypeAvailability>();
    }

    public class TicketTypeAvailability
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int AvailableCount { get; set; }
        public int ReservedCount { get; set; }
        public int SoldCount { get; set; }
    }

    public class ReservationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReservationId { get; set; } = string.Empty;
        public List<TicketResponse> Tickets { get; set; } = new List<TicketResponse>();
        public DateTime? ExpiresAt { get; set; }
        public decimal TotalPrice { get; set; }
        public TimeSpan? TimeRemaining { get; set; }
    }
} 