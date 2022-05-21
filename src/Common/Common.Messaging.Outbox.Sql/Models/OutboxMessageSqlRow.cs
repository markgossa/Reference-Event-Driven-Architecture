#nullable disable

namespace Common.Messaging.Outbox.Sql.Models;

public class OutboxMessageSqlRow
{
    public int Id { get; set; }
    public string CorrelationId { get; set; }
    public DateTime? LastAttempt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LockExpiry { get; set; }
    public string MessageBlob { get; set; }
    public DateTime? RetryAfter { get; set; }
}