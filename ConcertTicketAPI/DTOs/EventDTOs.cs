using System.ComponentModel.DataAnnotations;

namespace ConcertTicketAPI.DTOs
{
    public class CreateEventRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Venue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string VenueAddress { get; set; } = string.Empty;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int TotalCapacity { get; set; }
        
        public List<CreateTicketTypeRequest> TicketTypes { get; set; } = new List<CreateTicketTypeRequest>();
    }

    public class UpdateEventRequest
    {
        [StringLength(200, MinimumLength = 1)]
        public string? Title { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public DateTime? EventDate { get; set; }
        
        [StringLength(200, MinimumLength = 1)]
        public string? Venue { get; set; }
        
        [StringLength(500)]
        public string? VenueAddress { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? TotalCapacity { get; set; }
        
        public bool? IsActive { get; set; }
    }

    public class EventResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<TicketTypeResponse> TicketTypes { get; set; } = new List<TicketTypeResponse>();
    }

    public class CreateTicketTypeRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
    }

    public class TicketTypeResponse
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int AvailableCount { get; set; }
        public bool IsActive { get; set; }
    }
} 