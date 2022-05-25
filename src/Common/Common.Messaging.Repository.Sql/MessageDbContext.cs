#nullable disable

using Common;
using Common.Messaging.Repository.Sql.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Messaging.Repository.Sql;
public class MessageDbContext : DbContext
{
    public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
    {
    }

    public DbSet<MessageSqlRow> Messages { get; set; }
}
