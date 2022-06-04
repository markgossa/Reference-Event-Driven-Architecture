using Common.Messaging.Folder.Models;
using Common.Messaging.Folder.Repositories;
using Common.Messaging.Repository.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Common.Messaging.Repository.Sql;
public class SqlMessageRepository<T> : IMessageRepository<T>, IDisposable
{
    private readonly MessageDbContext _outboxMessageDbContext;
    private readonly ILogger<SqlMessageRepository<T>> _logger;

    public SqlMessageRepository(MessageDbContext outboxMessageDbContext, ILogger<SqlMessageRepository<T>> logger)
    {
        _outboxMessageDbContext = outboxMessageDbContext;
        _logger = logger;
    }

    public async Task AddAsync(Message<T> message)
    {
        try
        {
            await AddMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to SQL");
        }
    }

    public async Task UpdateAsync(IEnumerable<Message<T>> messages)
    {
        try
        {
            await UpdateMessagesWithinTransactionAsync(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating message in SQL");
        }
    }

    public async Task<IEnumerable<Message<T>>> GetAsync()
    {
        try
        {
            return await GetMessagesAsync(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed getting messages from SQL");
        }

        return new List<Message<T>>();
    }

    public async Task<IEnumerable<Message<T>>> GetAndLockAsync(int count)
    {
        using var transaction = await _outboxMessageDbContext.Database.BeginTransactionAsync();

        IEnumerable<Message<T>> messages = new List<Message<T>>();
        try
        {
            messages = await GetMessagesAsync(count);

            LockMessages(messages);

            await UpdateMessagesAsync(messages);

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed get and lock messages");
        }

        return messages;
    }

    public async Task CompleteAsync(IEnumerable<string> correlationIds)
    {
        foreach (var correlationId in correlationIds)
        {
            _outboxMessageDbContext.RemoveRange(
                _outboxMessageDbContext.Messages.Where(m => m.CorrelationId == correlationId));
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private async Task<IEnumerable<Message<T>>> GetMessagesAsync(int count)
        => await _outboxMessageDbContext.Messages
            .Where(m =>
                (m.LockExpiry == null || DateTime.UtcNow > m.LockExpiry)
                && (m.RetryAfter == null || DateTime.UtcNow > m.RetryAfter))
            .OrderBy(m => m.RetryAfter)
            .Take(count)
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

    private async Task AddMessageAsync(Message<T> message)
    {
        await _outboxMessageDbContext.AddAsync(MapToMessageSqlRow(message));
        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private async Task UpdateMessagesWithinTransactionAsync(IEnumerable<Message<T>> messages)
    {
        using var transaction = await _outboxMessageDbContext.Database.BeginTransactionAsync();
        await UpdateMessagesAsync(messages);
        await transaction.CommitAsync();
    }

    private async Task UpdateMessagesAsync(IEnumerable<Message<T>> messages)
    {
        foreach (var message in messages)
        {
            await UpdateMessagePropertiesAsync(message);
        }

        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private async Task UpdateMessagePropertiesAsync(Message<T> message)
    {
        var messageRow = await _outboxMessageDbContext.Messages
            .FirstOrDefaultAsync(m => m.CorrelationId == message.CorrelationId);
        if (messageRow is not null)
        {
            messageRow.AttemptCount = message.AttemptCount;
            messageRow.LastAttempt = message.LastAttempt;
            messageRow.LockExpiry = message.LockExpiry;
            messageRow.RetryAfter = message.RetryAfter;
        }
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

    private static void LockMessages(IEnumerable<Message<T>> messages)
    {
        foreach (var message in messages)
        {
            message.Lock();
        }
    }

    public void Dispose()
    {
        _outboxMessageDbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
