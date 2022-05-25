using Common.Messaging.Folder.Models;

namespace Common.Messaging.Folder.Repositories;
public interface IMessageRepository<T>
{
    Task AddAsync(Message<T> message);
    Task RemoveAsync(IEnumerable<string> correlationIds);
    Task<IEnumerable<Message<T>>> GetAsync();
    Task UpdateAsync(IEnumerable<Message<T>> messages);
}
