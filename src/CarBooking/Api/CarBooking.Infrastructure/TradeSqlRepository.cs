using LSE.Stocks.Application.Repositories;
using LSE.Stocks.Domain.Models.Shares;
using LSE.Stocks.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace LSE.Stocks.Infrastructure;

public class TradeSqlRepository : ITradeRepository, IDisposable, IAsyncDisposable
{
    private TradesDbContext? _dbContext;
    private readonly ILogger<TradeSqlRepository> _logger;
    private bool _disposedValue;

    public TradeSqlRepository(TradesDbContext dbContext, ILogger<TradeSqlRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SaveTradeAsync(Trade trade)
    {
        try
        {
            _dbContext!.Add(new TradeRow()
            {
                TickerSymbol = trade.TickerSymbol,
                BrokerId = trade.BrokerId,
                Count = trade.Count,
                Price = trade.Price
            });

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred saving trades");

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
                _dbContext?.Dispose();
            }

            _dbContext = null;
            _disposedValue = true;
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
