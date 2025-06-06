using Microsoft.AspNetCore.Mvc;
using ConcertTicketAPI.Services;
using ConcertTicketAPI.DTOs;

namespace ConcertTicketAPI.Controllers
{
    /// <summary>
    /// Event management endpoints for creating, reading, updating, and deleting concert events
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Retrieves all concert events
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive/cancelled events in the results</param>
        /// <returns>A list of all events with their ticket types and availability information</returns>
        /// <response code="200">Returns the list of events</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents([FromQuery] bool includeInactive = false)
        {
            var events = await _eventService.GetEventsAsync(includeInactive);
            return Ok(events);
        }



        /// <summary>
        /// Retrieves a specific concert event by its unique identifier
        /// </summary>
        /// <param name="id">The unique identifier of the event</param>
        /// <returns>The event details including all ticket types and current availability</returns>
        /// <response code="200">Returns the event details</response>
        /// <response code="404">Event not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponse>> GetEvent(int id)
        {
            var eventResponse = await _eventService.GetEventAsync(id);
            if (eventResponse == null)
            {
                return NotFound($"Event with ID {id} not found.");
            }

            return Ok(eventResponse);
        }

        /// <summary>
        /// Retrieves detailed ticket availability information for a specific event
        /// </summary>
        /// <param name="id">The unique identifier of the event</param>
        /// <returns>Comprehensive availability data including capacity, sold tickets, and reservations for each ticket type</returns>
        /// <response code="200">Returns the availability information</response>
        /// <response code="404">Event not found or not available</response>
        [HttpGet("{id}/availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TicketAvailabilityResponse>> GetEventAvailability(int id)
        {
            var availability = await _eventService.GetEventAvailabilityAsync(id);
            if (availability == null)
            {
                return NotFound($"Event with ID {id} not found or not available.");
            }

            return Ok(availability);
        }

        /// <summary>
        /// Creates a new concert event with ticket types
        /// </summary>
        /// <param name="request">The event creation request containing event details and ticket types</param>
        /// <returns>The created event with generated ID and ticket type information</returns>
        /// <response code="201">Event created successfully</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/events
        ///     {
        ///        "title": "Summer Music Festival 2024",
        ///        "description": "An amazing outdoor music festival",
        ///        "eventDate": "2024-07-15T18:00:00Z",
        ///        "venue": "Central Park",
        ///        "venueAddress": "New York, NY",
        ///        "totalCapacity": 5000,
        ///        "ticketTypes": [
        ///          {
        ///            "name": "VIP",
        ///            "description": "VIP access with backstage pass",
        ///            "price": 250.00,
        ///            "capacity": 100
        ///          }
        ///        ]
        ///     }
        ///
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate event date is in the future
            if (request.EventDate <= DateTime.UtcNow)
            {
                return BadRequest("Event date must be in the future.");
            }

            // Validate total capacity matches sum of ticket type capacities
            if (request.TicketTypes.Any())
            {
                var totalTicketCapacity = request.TicketTypes.Sum(tt => tt.Capacity);
                if (totalTicketCapacity > request.TotalCapacity)
                {
                    return BadRequest("Sum of ticket type capacities cannot exceed total event capacity.");
                }
            }

            var eventResponse = await _eventService.CreateEventAsync(request);
            return CreatedAtAction(nameof(GetEvent), new { id = eventResponse.Id }, eventResponse);
        }

        /// <summary>
        /// Updates an existing concert event
        /// </summary>
        /// <param name="id">The unique identifier of the event to update</param>
        /// <param name="request">The event update request with modified fields</param>
        /// <returns>The updated event information</returns>
        /// <response code="200">Event updated successfully</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <response code="404">Event not found</response>
        /// <remarks>
        /// Only the fields provided in the request will be updated. 
        /// Ticket types must be managed separately through their own endpoints.
        /// Event date must be in the future if provided.
        /// </remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate event date is in the future if provided
            if (request.EventDate.HasValue && request.EventDate <= DateTime.UtcNow)
            {
                return BadRequest("Event date must be in the future.");
            }

            var eventResponse = await _eventService.UpdateEventAsync(id, request);
            if (eventResponse == null)
            {
                return NotFound($"Event with ID {id} not found.");
            }

            return Ok(eventResponse);
        }

        /// <summary>
        /// Deletes or deactivates a concert event
        /// </summary>
        /// <param name="id">The unique identifier of the event to delete</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Event deleted/deactivated successfully</response>
        /// <response code="404">Event not found</response>
        /// <remarks>
        /// If tickets have been sold for the event, it will be marked as inactive instead of being permanently deleted.
        /// If no tickets have been sold, the event will be permanently removed from the database.
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var success = await _eventService.DeleteEventAsync(id);
            if (!success)
            {
                return NotFound($"Event with ID {id} not found.");
            }

            return NoContent();
        }
    }
} 