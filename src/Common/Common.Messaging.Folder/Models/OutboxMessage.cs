﻿namespace Common.Messaging.Folder.Models;

public class Message<T>
{
    public string CorrelationId { get; }
    public T MessageObject { get; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttempt { get; set; }
    public DateTime? LockExpiry { get; set; }
    public DateTime? RetryAfter { get; set; }

    private const int _lockDuration = 30;

    public Message(string correlationId, T messageObject)
    {
        CorrelationId = correlationId;
        MessageObject = messageObject;
        SetLock();
    }

    public void Lock() => SetLock();

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
