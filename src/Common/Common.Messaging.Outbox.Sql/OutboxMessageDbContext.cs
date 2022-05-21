#nullable disable

using Common.Messaging.Outbox.Sql.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Messaging.Outbox.Sql;
public class OutboxMessageDbContext : DbContext
{
    public OutboxMessageDbContext(DbContextOptions<OutboxMessageDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessageSqlRow> Messages { get; set; }
}
