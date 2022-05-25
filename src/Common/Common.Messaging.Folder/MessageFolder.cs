using Common.Messaging.Folder.Models;
using Common.Messaging.Folder.Repositories;

namespace Common.Messaging.Folder;
public class MessageFolder<T> : IMessageOutbox<T>, IMessageInbox<T>
{
    private readonly IMessageRepository<T> _messageRepository;

    public MessageFolder(IMessageRepository<T> messageRepository)
        => _messageRepository = messageRepository;

    public Task AddAsync(Message<T> message)
        => _messageRepository.AddAsync(message);

    public async Task<IEnumerable<Message<T>>> GetAsync()
    {
        var messages = await _messageRepository.GetAsync();
        LockMessages(messages);

        await _messageRepository.UpdateAsync(messages);

        return messages;
    }

    public Task RemoveAsync(IEnumerable<string> correlationIds)
        => _messageRepository.RemoveAsync(correlationIds);

    public Task FailAsync(IEnumerable<Message<T>> messages)
    {
        FailMessages(messages);

        return _messageRepository.UpdateAsync(messages);
    }

    private static void FailMessages(IEnumerable<Message<T>> messages)
    {
        foreach (var message in messages)
        {
            message.FailMessage();
        }
    }

    private static void LockMessages(IEnumerable<Message<T>> messages)
    {
        foreach (var message in messages)
        {
            message.Lock();
        }
    }
}
