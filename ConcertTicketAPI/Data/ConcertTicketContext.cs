using Microsoft.EntityFrameworkCore;
using ConcertTicketAPI.Models;

namespace ConcertTicketAPI.Data
{
    public class ConcertTicketContext : DbContext
    {
        public ConcertTicketContext(DbContextOptions<ConcertTicketContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Event configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Venue).IsRequired().HasMaxLength(200);
                entity.Property(e => e.VenueAddress).HasMaxLength(500);
                entity.Property(e => e.TotalCapacity).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                entity.HasIndex(e => e.EventDate);
                entity.HasIndex(e => e.Venue);
            });

            // TicketType configuration
            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Description).HasMaxLength(500);
                entity.Property(t => t.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(t => t.Capacity).IsRequired();
                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.UpdatedAt).IsRequired();

                entity.HasOne(t => t.Event)
                      .WithMany(e => e.TicketTypes)
                      .HasForeignKey(t => t.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(t => new { t.EventId, t.Name }).IsUnique();
            });

            // Ticket configuration
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(t => t.CustomerEmail).IsRequired().HasMaxLength(200);
                entity.Property(t => t.CustomerPhone).HasMaxLength(20);
                entity.Property(t => t.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(t => t.TicketNumber).IsRequired().HasMaxLength(50);
                entity.Property(t => t.Notes).HasMaxLength(500);
                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.UpdatedAt).IsRequired();

                entity.HasOne(t => t.Event)
                      .WithMany(e => e.Tickets)
                      .HasForeignKey(t => t.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.TicketType)
                      .WithMany(tt => tt.Tickets)
                      .HasForeignKey(t => t.TicketTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.TicketNumber).IsUnique();
                entity.HasIndex(t => t.CustomerEmail);
                entity.HasIndex(t => new { t.EventId, t.Status });
            });

            // Configure enum conversion
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<string>();
        }
    }
} 