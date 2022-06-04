﻿using Common.Messaging.Folder.Models;
using Common.Messaging.Folder.Repositories;

namespace Common.Messaging.Folder;
public class MessageFolder<T> : IMessageOutbox<T>, IMessageInbox<T>
{
    private readonly IMessageRepository<T> _messageRepository;

    public MessageFolder(IMessageRepository<T> messageRepository)
        => _messageRepository = messageRepository;

    public Task AddAsync(Message<T> message)
        => _messageRepository.AddAsync(message);

    public Task<IEnumerable<Message<T>>> GetAsync() => _messageRepository.GetAsync();
    
    public Task<IEnumerable<Message<T>>> GetAndLockAsync(int count) => _messageRepository.GetAndLockAsync(count);

    public Task CompleteAsync(IEnumerable<string> correlationIds) => _messageRepository.CompleteAsync(correlationIds);

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
}
