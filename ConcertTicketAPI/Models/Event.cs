using System.ComponentModel.DataAnnotations;

namespace ConcertTicketAPI.Models
{
    public class Event
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Venue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string VenueAddress { get; set; } = string.Empty;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int TotalCapacity { get; set; }
        
        public int AvailableCapacity { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
} 