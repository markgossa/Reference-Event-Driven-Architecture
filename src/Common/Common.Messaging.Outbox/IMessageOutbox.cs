using Common.Messaging.Outbox.Models;

namespace Common.Messaging.Outbox;
public interface IMessageOutbox<T>
{
    Task AddAsync(OutboxMessage<T> outboxMessage);
    Task RemoveAsync(IEnumerable<string> correlationIds);
    Task FailAsync(IEnumerable<string> correlationIds);
    Task<IEnumerable<OutboxMessage<T>>> GetAsync();
}
