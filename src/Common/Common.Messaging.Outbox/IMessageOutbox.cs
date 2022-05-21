using Common.Messaging.Outbox.Models;

namespace Common.Messaging.Outbox;
public interface IMessageOutbox<T>
{
    Task AddAsync(OutboxMessage<T> outboxMessage);
    Task RemoveAsync(IEnumerable<string> correlationIds);
    Task FailAsync(IEnumerable<OutboxMessage<T>> messages);
    Task<IEnumerable<OutboxMessage<T>>> GetAsync();
}
