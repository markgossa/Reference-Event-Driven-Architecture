using Common.Messaging.Folder.Models;

namespace Common.Messaging.Folder;
public interface IMessageFolder<T>
{
    Task AddAsync(Message<T> outboxMessage);
    Task CompleteAsync(IEnumerable<Message<T>> messages);
    Task FailAsync(IEnumerable<Message<T>> messages);
    Task<IEnumerable<Message<T>>> GetAsync();
    Task<IEnumerable<Message<T>>> GetAndLockAsync(int count);
}
