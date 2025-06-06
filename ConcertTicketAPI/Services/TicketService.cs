using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ConcertTicketAPI.Data;
using ConcertTicketAPI.Models;
using ConcertTicketAPI.DTOs;

namespace ConcertTicketAPI.Services
{
    public class TicketService : ITicketService
    {
        private readonly ConcertTicketContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _reservationExpiryTime = TimeSpan.FromMinutes(15); // 15 minutes to complete purchase
        private const string RESERVATION_PREFIX = "reservation_";
        private const string AVAILABILITY_PREFIX = "availability_";

        public TicketService(ConcertTicketContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ReservationResponse> ReserveTicketsAsync(ReserveTicketRequest request)
        {
            // Get ticket type with event - no transaction needed for reading
            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt => tt.Id == request.TicketTypeId && tt.IsActive);

            if (ticketType == null)
            {
                return new ReservationResponse
                {
                    Success = false,
                    Message = "Ticket type not found or not available."
                };
            }

            // Check if event is active and not in the past
            if (!ticketType.Event.IsActive || ticketType.Event.EventDate <= DateTime.UtcNow)
            {
                return new ReservationResponse
                {
                    Success = false,
                    Message = "Event is not available for booking."
                };
            }

            // Check availability with cache-aware logic
            var currentAvailability = await GetCachedAvailabilityAsync(request.TicketTypeId);
            if (currentAvailability < request.Quantity)
            {
                return new ReservationResponse
                {
                    Success = false,
                    Message = $"Only {currentAvailability} tickets available for {ticketType.Name}."
                };
            }

            // Create reservation in cache
            var reservationId = GenerateReservationId();
            var expiresAt = DateTime.UtcNow.Add(_reservationExpiryTime);
            var ticketNumbers = new List<string>();
            
            for (var i = 0; i < request.Quantity; i++)
            {
                ticketNumbers.Add(GenerateTicketNumber());
            }

            var cachedReservation = new CachedReservation
            {
                ReservationId = reservationId,
                EventId = request.EventId,
                TicketTypeId = request.TicketTypeId,
                Quantity = request.Quantity,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                PricePerTicket = ticketType.Price,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Notes = request.Notes,
                TicketNumbers = ticketNumbers
            };

            // Store in cache with TTL
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
                Priority = CacheItemPriority.High,
                Size = 1
            };
            
            // Set callback to restore availability when reservation expires
            cacheOptions.RegisterPostEvictionCallback(async (key, value, reason, state) =>
            {
                if (reason == EvictionReason.Expired && value is CachedReservation expiredReservation)
                {
                    await RestoreAvailabilityAsync(expiredReservation.TicketTypeId, expiredReservation.Quantity);
                }
            });

            _cache.Set($"{RESERVATION_PREFIX}{reservationId}", cachedReservation, cacheOptions);

            // Update cached availability
            await UpdateCachedAvailabilityAsync(request.TicketTypeId, -request.Quantity);

            // Create response tickets (these are virtual until purchased)
            var responseTickets = ticketNumbers.Select((ticketNumber, _) => new TicketResponse
            {
                Id = 0, // Not in database yet
                EventId = request.EventId,
                EventTitle = ticketType.Event.Title,
                TicketTypeId = request.TicketTypeId,
                TicketTypeName = ticketType.Name,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                Status = TicketStatus.Reserved,
                Price = ticketType.Price,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Notes = request.Notes,
                TicketNumber = ticketNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            return new ReservationResponse
            {
                Success = true,
                Message = $"Successfully reserved {request.Quantity} ticket(s). Please complete purchase within 15 minutes.",
                ReservationId = reservationId,
                Tickets = responseTickets,
                ExpiresAt = expiresAt,
                TotalPrice = cachedReservation.TotalPrice,
                TimeRemaining = cachedReservation.TimeRemaining
            };
        }

        public async Task<bool> PurchaseTicketsAsync(PurchaseTicketRequest request)
        {
            // Get reservation from cache
            var cachedReservation = await GetReservationAsync(request.ReservationId);
            if (cachedReservation == null)
            {
                return false; // Reservation not found or expired
            }

            if (cachedReservation.IsExpired)
            {
                // Remove expired reservation from cache
                _cache.Remove($"{RESERVATION_PREFIX}{request.ReservationId}");
                return false;
            }

            ///
            /// Could add future payment processing here e.g. integration with stripe
            ///

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get ticket type to verify current price and availability
                var ticketType = await _context.TicketTypes
                    .Include(tt => tt.Event)
                    .FirstOrDefaultAsync(tt => tt.Id == cachedReservation.TicketTypeId && tt.IsActive);

                if (ticketType == null || !ticketType.Event.IsActive)
                {
                    return false;
                }

                // Create actual tickets in database
                var tickets = new List<Ticket>();
                for (int i = 0; i < cachedReservation.Quantity; i++)
                {
                    var ticket = new Ticket
                    {
                        EventId = cachedReservation.EventId,
                        TicketTypeId = cachedReservation.TicketTypeId,
                        CustomerName = cachedReservation.CustomerName,
                        CustomerEmail = cachedReservation.CustomerEmail,
                        CustomerPhone = cachedReservation.CustomerPhone,
                        Status = TicketStatus.Purchased, // Directly to purchased status
                        Price = cachedReservation.PricePerTicket,
                        ReservedAt = cachedReservation.ReservedAt,
                        PurchasedAt = DateTime.UtcNow,
                        ExpiresAt = null, // No expiry for purchased tickets
                        Notes = string.IsNullOrEmpty(request.Notes) 
                            ? cachedReservation.Notes 
                            : $"{cachedReservation.Notes}; {request.Notes}",
                        TicketNumber = cachedReservation.TicketNumbers[i],
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    tickets.Add(ticket);
                    _context.Tickets.Add(ticket);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Remove reservation from cache (purchased successfully)
                _cache.Remove($"{RESERVATION_PREFIX}{request.ReservationId}");

                // Note: Cached availability is already decremented during reservation
                // and should remain that way since tickets are now permanently sold
                // The UpdateCachedAvailabilityAsync(-quantity) was called during ReserveTicketsAsync
                // and we don't need to call it again here

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public Task<CachedReservation?> GetReservationAsync(string reservationId)
        {
            if (_cache.TryGetValue($"{RESERVATION_PREFIX}{reservationId}", out CachedReservation? reservation))
            {
                return Task.FromResult(reservation);
            }
            return Task.FromResult<CachedReservation?>(null);
        }

        public async Task<bool> CancelReservationAsync(string reservationId)
        {
            // Get reservation from cache
            var cachedReservation = await GetReservationAsync(reservationId);
            if (cachedReservation == null)
            {
                return false; // Reservation not found or already expired
            }

            // Remove reservation from cache
            _cache.Remove($"{RESERVATION_PREFIX}{reservationId}");

            // Restore availability in cache
            await UpdateCachedAvailabilityAsync(cachedReservation.TicketTypeId, cachedReservation.Quantity);

            return true;
        }

        public async Task<TicketResponse?> GetTicketAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(t => t.Id == id);

            return ticket == null ? null : MapToTicketResponse(ticket, ticket.Event.Title, ticket.TicketType.Name);
        }



        public async Task<IEnumerable<TicketResponse>> GetTicketsByEventAsync(int eventId)
        {
            var tickets = await _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.TicketType)
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            return tickets.Select(t => MapToTicketResponse(t, t.Event.Title, t.TicketType.Name));
        }

        public Task CleanupExpiredReservationsAsync()
        {
            // This method is less critical now since cache handles TTL automatically
            // But we can use it to clean up any stale cache entries if needed
            return Task.CompletedTask;
        }

        public async Task<bool> IsTicketAvailableAsync(int ticketTypeId, int quantity)
        {
            var availableCount = await GetCachedAvailabilityAsync(ticketTypeId);
            return availableCount >= quantity;
        }

        private async Task<int> GetCachedAvailabilityAsync(int ticketTypeId)
        {
            var cacheKey = $"{AVAILABILITY_PREFIX}{ticketTypeId}";
            
            if (_cache.TryGetValue(cacheKey, out int cachedAvailability))
            {
                return cachedAvailability;
            }

            // Load from database and cache it
            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId && tt.IsActive);

            if (ticketType == null || !ticketType.Event.IsActive)
            {
                return 0;
            }

            // Calculate actual availability (database count minus sold tickets)
            var soldCount = await _context.Tickets
                .Where(t => t.TicketTypeId == ticketTypeId && t.Status == TicketStatus.Purchased)
                .CountAsync();

            var actualAvailability = ticketType.Capacity - soldCount;

            // Cache the availability
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), // Refresh every 5 minutes
                Priority = CacheItemPriority.Normal,
                Size = 1
            };

            _cache.Set(cacheKey, actualAvailability, cacheOptions);
            return actualAvailability;
        }

        private async Task UpdateCachedAvailabilityAsync(int ticketTypeId, int change)
        {
            var cacheKey = $"{AVAILABILITY_PREFIX}{ticketTypeId}";
            
            if (_cache.TryGetValue(cacheKey, out int currentAvailability))
            {
                var newAvailability = Math.Max(0, currentAvailability + change);
                
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal,
                    Size = 1
                };
                
                _cache.Set(cacheKey, newAvailability, cacheOptions);
            }
            else
            {
                // If not in cache, reload from database
                await GetCachedAvailabilityAsync(ticketTypeId);
            }
        }

        private Task RestoreAvailabilityAsync(int ticketTypeId, int quantity)
        {
            return UpdateCachedAvailabilityAsync(ticketTypeId, quantity);
        }

        private static string GenerateReservationId()
        {
            return $"RES_{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString()[..8]}";
        }

        private static string GenerateTicketNumber()
        {
            return $"TCK{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString()[..8]}";
        }

        private static TicketResponse MapToTicketResponse(Ticket ticket, string eventTitle, string ticketTypeName)
        {
            return new TicketResponse
            {
                Id = ticket.Id,
                EventId = ticket.EventId,
                EventTitle = eventTitle,
                TicketTypeId = ticket.TicketTypeId,
                TicketTypeName = ticketTypeName,
                CustomerName = ticket.CustomerName,
                CustomerEmail = ticket.CustomerEmail,
                CustomerPhone = ticket.CustomerPhone,
                Status = ticket.Status,
                Price = ticket.Price,
                ReservedAt = ticket.ReservedAt,
                PurchasedAt = ticket.PurchasedAt,
                ExpiresAt = ticket.ExpiresAt,
                CancelledAt = ticket.CancelledAt,
                Notes = ticket.Notes,
                TicketNumber = ticket.TicketNumber,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt
            };
        }
    }
}