using LSE.Stocks.Domain.Models.Shares;

namespace LSE.Stocks.Application.Repositories;

public interface ITradeRepository
{
    Task SaveTradeAsync(Trade trade);
}
