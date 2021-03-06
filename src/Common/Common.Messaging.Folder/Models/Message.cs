namespace Common.Messaging.Folder.Models;

public class Message<T>
{
    public string CorrelationId { get; }
    public T MessageObject { get; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttempt { get; set; }
    public DateTime? LockExpiry { get; set; }
    public DateTime? RetryAfter { get; set; }
    public DateTime? CompletedOn { get; set; }
    public string? MessageType { get; set; }
    private const int _lockDuration = 30;

    public Message(string correlationId, T messageObject)
    {
        CorrelationId = correlationId;
        MessageObject = messageObject;
        MessageType = typeof(T).Name;
    }

    public void Lock() => SetLock();

    internal void CompleteMessage() => CompletedOn = DateTime.UtcNow;

    internal void FailMessage()
    {
        RetryAfter = SetExponentialBackOff();
        AttemptCount++;
        LastAttempt = DateTime.UtcNow;
        LockExpiry = null;
    }

    private void SetLock()
        => LockExpiry = DateTime.UtcNow.AddSeconds(_lockDuration);

    private DateTime SetExponentialBackOff()
        => DateTime.UtcNow.AddSeconds(Math.Pow(2, AttemptCount));
}
