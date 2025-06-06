using Microsoft.EntityFrameworkCore;
using ConcertTicketAPI.Data;
using ConcertTicketAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with SQLite Database
builder.Services.AddDbContext<ConcertTicketContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=ConcertTickets.db"));

// Add Memory Cache for ticket reservations
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Concert Ticket Management API", 
        Version = "v1",
        Description = "A comprehensive API for managing concert events, tickets, and reservations"
    });
    
    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Concert Ticket Management API v1");
        c.RoutePrefix = string.Empty; // Make Swagger UI the root page
    });
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize database with sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ConcertTicketContext>();
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Seed data if database is empty
    await SeedData(context);
}

app.Run();

// Method to seed initial data
static async Task SeedData(ConcertTicketContext context)
{
    if (await context.Events.AnyAsync())
        return; // Database already seeded

    var events = new[]
    {
        new ConcertTicketAPI.Models.Event
        {
            Title = "Rock Concert 2024",
            Description = "Amazing rock concert featuring top bands",
            EventDate = DateTime.UtcNow.AddDays(30),
            Venue = "Madison Square Garden",
            VenueAddress = "4 Pennsylvania Plaza, New York, NY 10001",
            TotalCapacity = 1000,
            AvailableCapacity = 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.Event
        {
            Title = "Jazz Night",
            Description = "Smooth jazz evening with renowned artists",
            EventDate = DateTime.UtcNow.AddDays(45),
            Venue = "Blue Note",
            VenueAddress = "131 W 3rd St, New York, NY 10012",
            TotalCapacity = 300,
            AvailableCapacity = 300,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.Event
        {
            Title = "Pop Extravaganza",
            Description = "The biggest pop stars in one night",
            EventDate = DateTime.UtcNow.AddDays(60),
            Venue = "Staples Center",
            VenueAddress = "1111 S Figueroa St, Los Angeles, CA 90015",
            TotalCapacity = 2000,
            AvailableCapacity = 2000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        }
    };

    context.Events.AddRange(events);
    await context.SaveChangesAsync();

    // Add ticket types for the events
    var ticketTypes = new[]
    {
        // Rock Concert tickets
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 1,
            Name = "VIP",
            Description = "VIP seating with backstage access",
            Price = 150.00m,
            Capacity = 50,
            AvailableCount = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 1,
            Name = "General Admission",
            Description = "General admission standing",
            Price = 75.00m,
            Capacity = 700,
            AvailableCount = 700,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 1,
            Name = "Balcony",
            Description = "Balcony seating",
            Price = 50.00m,
            Capacity = 250,
            AvailableCount = 250,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        
        // Jazz Night tickets
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 2,
            Name = "Premium Table",
            Description = "Table seating with dinner service",
            Price = 120.00m,
            Capacity = 40,
            AvailableCount = 40,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 2,
            Name = "Standard",
            Description = "Standard seating",
            Price = 60.00m,
            Capacity = 260,
            AvailableCount = 260,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        
        // Pop Extravaganza tickets
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 3,
            Name = "VIP Package",
            Description = "VIP package with meet & greet",
            Price = 300.00m,
            Capacity = 100,
            AvailableCount = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 3,
            Name = "Premium",
            Description = "Premium seating near stage",
            Price = 150.00m,
            Capacity = 400,
            AvailableCount = 400,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        },
        new ConcertTicketAPI.Models.TicketType
        {
            EventId = 3,
            Name = "General",
            Description = "General admission",
            Price = 80.00m,
            Capacity = 1500,
            AvailableCount = 1500,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        }
    };

    context.TicketTypes.AddRange(ticketTypes);
    await context.SaveChangesAsync();
}
