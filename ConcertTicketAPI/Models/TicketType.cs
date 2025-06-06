using System.ComponentModel.DataAnnotations;

namespace ConcertTicketAPI.Models
{
    public class TicketType
    {
        public int Id { get; set; }
        
        public int EventId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "VIP", "General Admission", "Balcony"
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
        
        public int AvailableCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
} 