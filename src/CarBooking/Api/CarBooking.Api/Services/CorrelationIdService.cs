namespace LSE.Stocks.Api.Services;

public class CorrelationIdService : ICorrelationIdService
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
