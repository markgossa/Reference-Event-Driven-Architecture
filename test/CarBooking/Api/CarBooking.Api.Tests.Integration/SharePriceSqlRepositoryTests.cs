using LSE.Stocks.Api.Tests.Integration;
using LSE.Stocks.Domain.Models.Shares;
using LSE.Stocks.Infrastructure;
using LSE.Stocks.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarBooking.Api.Tests.Integration;

public class SharePriceSqlRepositoryTests : TradeSqlRepositoryTestsBase
{
    [Fact]
    public async Task GivenThereIsATradeInTheTradesTable_WhenIGetTrades_ThenAllTradesAreReturned()
    {
        const string tickerSymbol = "MSFT";
        await SeedDatabaseAsync(BuildTrades());

        var actualTrades = await GetTradesFromSharePriceSqlRepositoryAsync(tickerSymbol);

        AssertCorrectTradeDetailsReturned(tickerSymbol, actualTrades);
    }

    [Fact]
    public async Task GivenThereIsAreNoTradesInTheTradesTable_WhenIGetTrades_ThenAnEmptyIEnumerableIsReturned()
    {
        const string tickerSymbol = "MSFT";

        var actualTrades = await GetTradesFromSharePriceSqlRepositoryAsync(tickerSymbol);

        Assert.Empty(actualTrades);
    }

    private static List<TradeRow> BuildTrades()
        => new()
        {
            new ()
            {
                TickerSymbol = "MSFT",
                Price = 7.5m,
                Count = 7,
                BrokerId = Guid.NewGuid().ToString()
            },
            new ()
            {
                TickerSymbol = "MSFT",
                Price = 8m,
                Count = 5,
                BrokerId = Guid.NewGuid().ToString()
            },
            new ()
            {
                TickerSymbol = "APPL",
                Price = 5.5m,
                Count = 3,
                BrokerId = Guid.NewGuid().ToString()
            }
        };

    private async Task SeedDatabaseAsync(List<TradeRow> trades) => await AddRecordsToInMemoryDatabase(trades);

    private async Task<IEnumerable<Trade>> GetTradesFromSharePriceSqlRepositoryAsync(string tickerSymbol)
    {
        var sut = new SharePriceSqlRepository(_tradesDbContext!, new Mock<ILogger<SharePriceSqlRepository>>().Object);

        return await sut.GetTradesAsync(tickerSymbol);
    }

    private void AssertCorrectTradeDetailsReturned(string tickerSymbol, IEnumerable<Trade> actualTrades)
        => Assert.Equal(GetMatchingTradesFromDatabase(tickerSymbol), actualTrades);

    private IEnumerable<Trade> GetMatchingTradesFromDatabase(string tickerSymbol)
        => _tradesDbContext!.Trades.Where(t => t.TickerSymbol == tickerSymbol)
            .Select(t => new Trade(t.TickerSymbol, t.Price, t.Count, t.BrokerId));
}
