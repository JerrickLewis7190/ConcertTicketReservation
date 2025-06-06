using Microsoft.AspNetCore.Mvc;
using ConcertTicketAPI.Services;
using ConcertTicketAPI.DTOs;
using ConcertTicketAPI.Models;

namespace ConcertTicketAPI.Controllers
{
    /// <summary>
    /// Ticket management endpoints for reserving, purchasing, and managing concert tickets
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IEventService _eventService;

        public TicketsController(ITicketService ticketService, IEventService eventService)
        {
            _ticketService = ticketService;
            _eventService = eventService;
        }

        /// <summary>
        /// Reserves tickets for a concert event with a 15-minute hold period
        /// </summary>
        /// <param name="request">The ticket reservation request containing event, ticket type, quantity, and customer details</param>
        /// <returns>A reservation response with reservation ID, ticket details, and expiration time</returns>
        /// <response code="200">Tickets reserved successfully</response>
        /// <response code="400">Invalid request data, insufficient availability, or event not available</response>
        /// <remarks>
        /// Reserves tickets in memory cache with a 15-minute expiration window.
        /// The reservation must be purchased within this time or it will automatically expire.
        /// Maximum 10 tickets per reservation.
        /// 
        /// Sample request:
        ///
        ///     POST /api/tickets/reserve
        ///     {
        ///        "eventId": 1,
        ///        "ticketTypeId": 1,
        ///        "quantity": 2,
        ///        "customerName": "John Doe",
        ///        "customerEmail": "john.doe@example.com",
        ///        "customerPhone": "+1-555-123-4567",
        ///        "notes": "Anniversary celebration"
        ///     }
        ///
        /// </remarks>
        [HttpPost("reserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationResponse>> ReserveTickets([FromBody] ReserveTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify event exists and is available
            var eventExists = await _eventService.EventExistsAsync(request.EventId);
            if (!eventExists)
            {
                return BadRequest("Invalid event ID.");
            }

            // Check ticket availability before attempting reservation
            var isAvailable = await _ticketService.IsTicketAvailableAsync(request.TicketTypeId, request.Quantity);
            if (!isAvailable)
            {
                return BadRequest("Requested tickets are not available.");
            }

            var reservation = await _ticketService.ReserveTicketsAsync(request);
            
            if (!reservation.Success)
            {
                return BadRequest(reservation.Message);
            }

            return Ok(reservation);
        }

        /// <summary>
        /// Retrieves details of an active ticket reservation from cache
        /// </summary>
        /// <param name="reservationId">The unique reservation identifier returned from the reserve endpoint</param>
        /// <returns>Complete reservation details including tickets, pricing, and expiration information</returns>
        /// <response code="200">Reservation found and details returned</response>
        /// <response code="400">Invalid reservation ID format or reservation has expired</response>
        /// <response code="404">Reservation not found</response>
        /// <remarks>
        /// Only active reservations (not yet purchased or expired) are available through this endpoint.
        /// Expired reservations are automatically removed from cache.
        /// </remarks>
        [HttpGet("reservation/{reservationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CachedReservation>> GetReservation(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return BadRequest("Reservation ID is required.");
            }

            var reservation = await _ticketService.GetReservationAsync(reservationId);
            if (reservation == null)
            {
                return NotFound($"Reservation with ID {reservationId} not found or has expired.");
            }

            if (reservation.IsExpired)
            {
                return BadRequest($"Reservation with ID {reservationId} has expired.");
            }

            return Ok(reservation);
        }

        /// <summary>
        /// Completes the purchase of previously reserved tickets
        /// </summary>
        /// <param name="request">The purchase request containing reservation ID and payment information</param>
        /// <returns>Confirmation of successful purchase</returns>
        /// <response code="200">Tickets purchased successfully and saved to database</response>
        /// <response code="400">Invalid request, reservation expired, or payment failed</response>
        /// <remarks>
        /// Moves tickets from cache to permanent database storage and marks them as purchased.
        /// The reservation is removed from cache upon successful purchase.
        /// Payment processing is simulated - integrate with real payment provider in production.
        /// 
        /// Sample request:
        ///
        ///     POST /api/tickets/purchase
        ///     {
        ///        "reservationId": "RES_1234567890",
        ///        "paymentReference": "stripe_ch_1234567890",
        ///        "notes": "Payment completed successfully"
        ///     }
        ///
        /// </remarks>
        [HttpPost("purchase")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PurchaseTickets([FromBody] PurchaseTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _ticketService.PurchaseTicketsAsync(request);
            if (!success)
            {
                return BadRequest("Unable to complete purchase. Reservation may have expired or is no longer available.");
            }

            return Ok(new { Message = "Tickets purchased successfully", PaymentReference = request.PaymentReference });
        }

        /// <summary>
        /// Cancels an active ticket reservation and restores availability
        /// </summary>
        /// <param name="reservationId">The unique reservation identifier to cancel</param>
        /// <returns>Confirmation of successful cancellation</returns>
        /// <response code="200">Reservation cancelled successfully</response>
        /// <response code="400">Invalid reservation ID format</response>
        /// <response code="404">Reservation not found or has already expired</response>
        /// <remarks>
        /// Removes the reservation from cache and immediately restores ticket availability.
        /// Cannot cancel reservations that have already been purchased.
        /// </remarks>
        [HttpDelete("reservation/{reservationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelReservation(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return BadRequest("Reservation ID is required.");
            }

            var success = await _ticketService.CancelReservationAsync(reservationId);
            
            if (!success)
            {
                return NotFound($"Reservation with ID {reservationId} not found or has already expired.");
            }

            return Ok(new { Message = "Reservation cancelled successfully", ReservationId = reservationId });
        }

        /// <summary>
        /// Retrieves a specific purchased ticket by its unique identifier
        /// </summary>
        /// <param name="id">The unique ticket identifier</param>
        /// <returns>Complete ticket details including customer information and status</returns>
        /// <response code="200">Ticket found and details returned</response>
        /// <response code="404">Ticket not found</response>
        /// <remarks>
        /// Only returns tickets that have been purchased and saved to the database.
        /// Reserved tickets (not yet purchased) are not accessible through this endpoint.
        /// </remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TicketResponse>> GetTicket(int id)
        {
            var ticket = await _ticketService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound($"Ticket with ID {id} not found.");
            }

            return Ok(ticket);
        }



        /// <summary>
        /// Retrieves all purchased tickets for a specific event (administrative endpoint)
        /// </summary>
        /// <param name="eventId">The unique identifier of the event</param>
        /// <returns>A list of all purchased tickets for the specified event</returns>
        /// <response code="200">Returns the list of tickets for the event</response>
        /// <response code="404">Event not found</response>
        /// <remarks>
        /// This is an administrative endpoint that returns all purchased tickets for an event.
        /// Only includes tickets that have been purchased and saved to the database.
        /// Reserved tickets (not yet purchased) are not included in the results.
        /// Useful for event management, check-in processes, and attendance tracking.
        /// </remarks>
        [HttpGet("event/{eventId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TicketResponse>>> GetTicketsByEvent(int eventId)
        {
            var eventExists = await _eventService.EventExistsAsync(eventId);
            if (!eventExists)
            {
                return NotFound($"Event with ID {eventId} not found.");
            }

            var tickets = await _ticketService.GetTicketsByEventAsync(eventId);
            return Ok(tickets);
        }

        /// <summary>
        /// Checks real-time availability for a specific ticket type
        /// </summary>
        /// <param name="ticketTypeId">The unique identifier of the ticket type</param>
        /// <param name="quantity">The number of tickets to check availability for (1-10)</param>
        /// <returns>Availability status and requested quantity information</returns>
        /// <response code="200">Returns availability status</response>
        /// <response code="400">Invalid quantity (must be between 1 and 10)</response>
        /// <remarks>
        /// Uses cache-aware logic to provide real-time availability that accounts for:
        /// - Sold tickets (permanently unavailable)
        /// - Active reservations (temporarily unavailable)
        /// - Expired reservations (restored to availability)
        /// 
        /// This endpoint is useful for real-time UI updates and pre-purchase validation.
        /// Maximum quantity check is 10 tickets per request.
        /// </remarks>
        [HttpGet("availability/{ticketTypeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> CheckTicketAvailability(int ticketTypeId, [FromQuery] int quantity = 1)
        {
            if (quantity <= 0 || quantity > 10)
            {
                return BadRequest("Quantity must be between 1 and 10.");
            }

            var isAvailable = await _ticketService.IsTicketAvailableAsync(ticketTypeId, quantity);
            return Ok(new { Available = isAvailable, TicketTypeId = ticketTypeId, RequestedQuantity = quantity });
        }
    }
}