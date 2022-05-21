using Common.Messaging.Outbox.Models;
using Common.Messaging.Outbox.Repositories;

namespace Common.Messaging.Outbox;
public class MessageOutbox<T> : IMessageOutbox<T>
{
    private readonly IOutboxMessageRepository<T> _outboxMessageRepository;

    public MessageOutbox(IOutboxMessageRepository<T> outboxMessageRepository)
        => _outboxMessageRepository = outboxMessageRepository;

    public Task AddAsync(OutboxMessage<T> outboxMessage) 
        => _outboxMessageRepository.AddAsync(outboxMessage);

    public async Task<IEnumerable<OutboxMessage<T>>> GetAsync()
    {
        var messages = await _outboxMessageRepository.GetAsync();
        LockMessages(messages);

        return messages;
    }

    public Task RemoveAsync(IEnumerable<string> correlationIds)
        => _outboxMessageRepository.RemoveAsync(correlationIds);

    public Task FailAsync(IEnumerable<OutboxMessage<T>> messages)
    {
        FailMessages(messages);

        return _outboxMessageRepository.UpdateAsync(messages);
    }

    private static void FailMessages(IEnumerable<OutboxMessage<T>> messages)
    {
        foreach (var message in messages)
        {
            message.FailMessage();
        }
    }

    private static void LockMessages(IEnumerable<OutboxMessage<T>> messages)
    {
        foreach (var message in messages)
        {
            message.Lock();
        }
    }
}
