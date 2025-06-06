using System.ComponentModel.DataAnnotations;

namespace ConcertTicketAPI.Models
{
    public enum TicketStatus
    {
        Reserved,
        Purchased,
        Cancelled,
        Expired
    }

    public class Ticket
    {
        public int Id { get; set; }
        
        public int EventId { get; set; }
        
        public int TicketTypeId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        
        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Reserved;
        
        public decimal Price { get; set; }
        
        public DateTime ReservedAt { get; set; }
        
        public DateTime? PurchasedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TicketNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual TicketType TicketType { get; set; } = null!;
    }
} 