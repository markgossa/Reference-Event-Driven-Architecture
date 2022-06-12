#nullable disable

using Microsoft.EntityFrameworkCore;

namespace LSE.Stocks.Infrastructure.Models;

public class TradesDbContext : DbContext
{
    public DbSet<TradeRow> Trades { get; set; }

    public TradesDbContext(DbContextOptions<TradesDbContext> options)
        : base(options)
    {
    }
}
