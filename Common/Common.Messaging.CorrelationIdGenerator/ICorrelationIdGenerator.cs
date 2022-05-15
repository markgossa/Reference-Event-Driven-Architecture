namespace Common.Messaging.CorrelationIdGenerator;

public interface ICorrelationIdGenerator
{
    public string CorrelationId { get; set; }
}
