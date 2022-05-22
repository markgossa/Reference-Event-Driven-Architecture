using Common.Messaging.Outbox.Models;
using Common.Messaging.Outbox.Repositories;
using Common.Messaging.Outbox.Sql.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Common.Messaging.Outbox.Sql;
public class SqlOutboxMessageRepository<T> : IOutboxMessageRepository<T>
{
    private readonly OutboxMessageDbContext _outboxMessageDbContext;

    public SqlOutboxMessageRepository(OutboxMessageDbContext outboxMessageDbContext)
        => _outboxMessageDbContext = outboxMessageDbContext;

    public async Task AddAsync(OutboxMessage<T> message)
    {
        await _outboxMessageDbContext.AddAsync(MapToOutboxMessageSqlRow(message));
        await _outboxMessageDbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(IEnumerable<OutboxMessage<T>> messages)
    {
        foreach (var message in messages)
        {
            await UpdateMessagePropertiesAsync(message);
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

#nullable disable
    public async Task<IEnumerable<OutboxMessage<T>>> GetAsync() 
        => await _outboxMessageDbContext.Messages
            .Where(m =>
                (m.LockExpiry == null || DateTime.UtcNow > m.LockExpiry)
                && (m.RetryAfter == null || DateTime.UtcNow > m.RetryAfter))
            .Select(m =>
                new OutboxMessage<T>(m.CorrelationId, 
                    JsonSerializer.Deserialize<T>(m.MessageBlob, new JsonSerializerOptions()))
                {
                    AttemptCount = m.AttemptCount,
                    LastAttempt = m.LastAttempt,
                    LockExpiry = m.LockExpiry,
                    RetryAfter = m.RetryAfter
                }
            ).ToListAsync();
#nullable enable

    public async Task RemoveAsync(IEnumerable<string> correlationIds)
    {
        foreach (var correlationId in correlationIds)
        {
            _outboxMessageDbContext.RemoveRange(
                _outboxMessageDbContext.Messages.Where(m => m.CorrelationId == correlationId));
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private static OutboxMessageSqlRow MapToOutboxMessageSqlRow(OutboxMessage<T> message)
        => new()
        {
            CorrelationId = message.CorrelationId,
            LastAttempt = message.LastAttempt,
            AttemptCount = message.AttemptCount,
            LockExpiry = message.LockExpiry,
            MessageBlob = JsonSerializer.Serialize(message.MessageObject)
        };

    private async Task UpdateMessagePropertiesAsync(OutboxMessage<T> message)
    {
        var messageRow = await _outboxMessageDbContext.Messages
            .FirstAsync(m => m.CorrelationId == message.CorrelationId);
        messageRow.AttemptCount = message.AttemptCount;
        messageRow.LastAttempt = message.LastAttempt;
        messageRow.LockExpiry = message.LockExpiry;
        messageRow.RetryAfter = message.RetryAfter;
    }
}
