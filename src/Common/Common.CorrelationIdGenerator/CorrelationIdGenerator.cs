namespace Common.CorrelationIdGenerator;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
