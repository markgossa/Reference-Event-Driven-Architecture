using Common.Messaging.Folder.Models;

namespace Common.Messaging.Folder.Repositories;
public interface IMessageRepository<T>
{
    Task AddAsync(Message<T> message);
    Task CompleteAsync(IEnumerable<string> correlationIds);
    Task<IEnumerable<Message<T>>> GetAsync();
    Task UpdateAsync(IEnumerable<Message<T>> messages);
    Task<IEnumerable<Message<T>>> GetAndLockAsync(int count);
}
