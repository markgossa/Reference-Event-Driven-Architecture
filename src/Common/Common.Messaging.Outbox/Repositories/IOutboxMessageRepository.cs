using Common.Messaging.Outbox.Models;

namespace Common.Messaging.Outbox.Repositories;
public interface IOutboxMessageRepository<T>
{
    Task AddAsync(OutboxMessage<T> message);
    Task RemoveAsync(IEnumerable<string> correlationIds);
    Task<IEnumerable<OutboxMessage<T>>> GetAsync();
    Task UpdateAsync(IEnumerable<OutboxMessage<T>> messages);
}
