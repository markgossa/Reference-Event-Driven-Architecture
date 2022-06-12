using LSE.Stocks.Application.Repositories;
using LSE.Stocks.Domain.Models.Shares;
using LSE.Stocks.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LSE.Stocks.Infrastructure;

public class SharePriceSqlRepository : ISharePriceRepository, IDisposable, IAsyncDisposable
{
    private TradesDbContext? _dbContext;
    private readonly ILogger<SharePriceSqlRepository> _logger;
    private bool _disposedValue;

    public SharePriceSqlRepository(TradesDbContext dbContext, ILogger<SharePriceSqlRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<IEnumerable<Trade>> GetTradesAsync(string tickerSymbol)
    {
        try
        {
            return Task.FromResult(_dbContext!.Trades
                .AsNoTracking()
                .Where(t => t.TickerSymbol.ToLower() == tickerSymbol.ToLower())
                .Select(t => new Trade(t.TickerSymbol, t.Price, t.Count, t.BrokerId))
                .AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trades from database");

            throw;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dbContext!.Dispose();
            }

            _disposedValue = true;
            _dbContext = null;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync().ConfigureAwait(false);
        }

        _dbContext = null;
    }
}
