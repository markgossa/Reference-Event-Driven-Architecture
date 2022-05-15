using Common.Messaging.Outbox.Enums;

namespace Common.Messaging.Outbox.Models;
public class OutboxMessage<T>
{
    public string CorrelationId { get; }
    public T MessageObject { get; }
    public OutboxMessage(string correlationId, T messageObject)
    {
        CorrelationId = correlationId;
        MessageObject = messageObject;
    }
}
