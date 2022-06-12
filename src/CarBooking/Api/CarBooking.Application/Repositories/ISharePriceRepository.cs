using LSE.Stocks.Domain.Models.Shares;

namespace LSE.Stocks.Application.Repositories;

public interface ISharePriceRepository
{
    Task<IEnumerable<Trade>> GetTradesAsync(string tickerSymbol);
}
