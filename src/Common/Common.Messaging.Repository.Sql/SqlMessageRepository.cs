using Common.Messaging.Folder.Models;
using Common.Messaging.Folder.Repositories;
using Common.Messaging.Repository.Sql.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Common.Messaging.Repository.Sql;
public class SqlMessageRepository<T> : IMessageRepository<T>
{
    private readonly MessageDbContext _outboxMessageDbContext;

    public SqlMessageRepository(MessageDbContext outboxMessageDbContext)
        => _outboxMessageDbContext = outboxMessageDbContext;

    public async Task AddAsync(Message<T> message)
    {
        await _outboxMessageDbContext.AddAsync(MapToMessageSqlRow(message));
        await _outboxMessageDbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(IEnumerable<Message<T>> messages)
    {
        foreach (var message in messages)
        {
            await UpdateMessagePropertiesAsync(message);
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Message<T>>> GetAsync()
        => await _outboxMessageDbContext.Messages
            .Where(m =>
                (m.LockExpiry == null || DateTime.UtcNow > m.LockExpiry)
                && (m.RetryAfter == null || DateTime.UtcNow > m.RetryAfter))
            .Select(m =>
                new Message<T>(m.CorrelationId,
                    JsonSerializer.Deserialize<T>(m.MessageBlob, new JsonSerializerOptions())!)
                {
                    AttemptCount = m.AttemptCount,
                    LastAttempt = m.LastAttempt,
                    LockExpiry = m.LockExpiry,
                    RetryAfter = m.RetryAfter
                }
            ).ToListAsync();

    public async Task RemoveAsync(IEnumerable<string> correlationIds)
    {
        foreach (var correlationId in correlationIds)
        {
            _outboxMessageDbContext.RemoveRange(
                _outboxMessageDbContext.Messages.Where(m => m.CorrelationId == correlationId));
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private static MessageSqlRow MapToMessageSqlRow(Message<T> message)
        => new()
        {
            CorrelationId = message.CorrelationId,
            LastAttempt = message.LastAttempt,
            AttemptCount = message.AttemptCount,
            LockExpiry = message.LockExpiry,
            MessageBlob = JsonSerializer.Serialize(message.MessageObject)
        };

    private async Task UpdateMessagePropertiesAsync(Message<T> message)
    {
        var messageRow = await _outboxMessageDbContext.Messages
            .FirstAsync(m => m.CorrelationId == message.CorrelationId);
        messageRow.AttemptCount = message.AttemptCount;
        messageRow.LastAttempt = message.LastAttempt;
        messageRow.LockExpiry = message.LockExpiry;
        messageRow.RetryAfter = message.RetryAfter;
    }
}
