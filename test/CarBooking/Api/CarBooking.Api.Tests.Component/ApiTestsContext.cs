using LSE.Stocks.Api.Services;
using LSE.Stocks.Application.Repositories;
using LSE.Stocks.Domain.Models.Shares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;

namespace LSE.Stocks.Api.Tests.Component;

public class ApiTestsContext : WebApplicationFactory<Startup>, IDisposable
{
    public Mock<ITradeRepository> MockTradeRepository { get; } = new();
    public HttpClient HttpClient { get; }
    private readonly Mock<ISharePriceRepository> _mockSharePriceRepository = new();
    private readonly Mock<ICorrelationIdService> _mockCorrelationIdService = new();
    public readonly string CorrelationId = Guid.NewGuid().ToString();

    public ApiTestsContext()
    {
        HttpClient = CreateClient();
        SetUpMockSharePricingRepository();
        SetUpMockTradeRepository();
        _mockCorrelationIdService.Setup(m => m.CorrelationId).Returns(CorrelationId);
    }

    private void SetUpMockSharePricingRepository()
    {
        _ = _mockSharePriceRepository.Setup(m => m.GetTradesAsync("NASDAQ:AAPL"))
            .ReturnsAsync(new List<Trade>()
                {
                    new ("NASDAQ:AAPL", 10, 2, null),
                    new ("NASDAQ:AAPL", 20, 4, null),
                });

        _ = _mockSharePriceRepository.Setup(m => m.GetTradesAsync("NASDAQ:TSLA"))
            .ReturnsAsync(new List<Trade>()
                {
                    new ("NASDAQ:TSLA", 150, 2, null),
                    new ("NASDAQ:TSLA", 300, 4, null)
                });

        _ = _mockSharePriceRepository.Setup(m => m.GetTradesAsync("NASDAQ:ERROR"))
            .Throws(new Exception("Something bad happened"));
    }

    private void SetUpMockTradeRepository()
        => MockTradeRepository.Setup(m => m.SaveTradeAsync(It.Is<Trade>(t => t.TickerSymbol == "NASDAQ:ERROR")))
            .ThrowsAsync(new Exception());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureServices(services =>
        {
            _ = ((ServiceCollection)services).AddSingleton(MockTradeRepository.Object);
            _ = ((ServiceCollection)services).AddSingleton(_mockSharePriceRepository.Object);
            _ = ((ServiceCollection)services).AddSingleton(_mockCorrelationIdService.Object);
        });

    public new void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}