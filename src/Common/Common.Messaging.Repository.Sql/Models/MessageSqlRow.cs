#nullable disable

using Common;
using Microsoft.EntityFrameworkCore;

namespace Common.Messaging.Repository.Sql.Models;

[Index(nameof(CorrelationId), IsUnique = true)]
public class MessageSqlRow
{
    public int Id { get; set; }
    public string CorrelationId { get; set; }
    public DateTime? LastAttempt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LockExpiry { get; set; }
    public string MessageBlob { get; set; }
    public DateTime? RetryAfter { get; set; }
    public DateTime? CompletedOn { get; set; }
}