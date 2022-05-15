using Common.Messaging.Outbox.Models;

namespace Common.Messaging.Outbox;
public interface IMessageOutbox<T>
{
    Task AddAsync(OutboxMessage<T> outboxMessage);
    Task RemoveAsync(string correlationId);
    Task FailAsync(string correlationId);
    Task<IEnumerable<OutboxMessage<T>>> GetAsync();
}
