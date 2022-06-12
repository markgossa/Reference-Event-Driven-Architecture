using LSE.Stocks.Domain.Models.Shares;
using LSE.Stocks.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using Xunit;

namespace LSE.Stocks.Api.Tests.Integration
{
    public class TradeSqlRepositoryTests : TradeSqlRepositoryTestsBase
    {
        [Theory]
        [InlineData("MSFT", 5.5, 10, "314523")]
        [InlineData("APPL", 4.5, 5, "43719")]
        public async void GivenNewTrade_WhenTradeIsSaved_ThenTradeIsSavedToDatabase(string tickerSymbol, 
            decimal price, decimal count, string brokerId)
        {
            var sut = new TradeSqlRepository(_tradesDbContext!, new Mock<ILogger<TradeSqlRepository>>().Object);
            await sut.SaveTradeAsync(new Trade(tickerSymbol, price, count, brokerId));

            Assert.Single(_tradesDbContext!.Trades);
            var actualTrade = _tradesDbContext.Trades.First();
            Assert.Equal(tickerSymbol, actualTrade.TickerSymbol);
            Assert.Equal(price, actualTrade.Price);
            Assert.Equal(count, actualTrade.Count);
            Assert.Equal(brokerId, actualTrade.BrokerId);
        }
    }
}
