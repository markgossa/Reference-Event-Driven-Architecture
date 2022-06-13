#nullable disable

using Microsoft.EntityFrameworkCore;

namespace CarBooking.Infrastructure.Models;
public class CarBookingDbContext : DbContext
{
    public CarBookingDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<CarBookingRow> CarBookings { get; set; }
}
