using LSE.Stocks.Api.Models;
using LSE.Stocks.Domain.Models.Shares;
using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LSE.Stocks.Api.Tests.Component.Common;

public class SaveTradeTests : IClassFixture<ApiTestsContext>
{
    private const string _apiRoute = "trades";
    private const string _apiRouteV1 = "v1/trades";
    private const string _apiRouteV2 = "v2/trades";
    private readonly ApiTestsContext _context;

    public SaveTradeTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("NASDAQ:AAPL", 10, 1, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:TSLA", 25.05, 2, "BR00432", _apiRoute)]
    [InlineData("NASDAQ:AAPL", 10, 1, "CR4314", _apiRouteV1)]
    [InlineData("NASDAQ:TSLA", 25.05, 2, "CR4314", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", 10, 1, "DR4314", _apiRouteV2)]
    [InlineData("NASDAQ:TSLA", 25.05, 2, "DR4314", _apiRouteV2)]
    public async Task GivenValidTradeRequest_WhenPostEndpointCalled_ThenSavesTradeAndReturnsOK(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
    {
        var tradeRequest = new SaveTradeRequest(tickerSymbol, price, count, brokerId);
        var httpResponse = await PostTradeAsync(tradeRequest, apiRoute);
        var tradeResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        AssertTradeSaved(tradeRequest);
        AssertTradeResponseEqualToTradeRequest(tradeRequest, tradeResponse);
    }
    
    [Theory]
    [InlineData("NASDAQ:AAPL", 10, 1, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:AAPL", 10, 1, "CR4314", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", 10, 1, "DR4314", _apiRouteV2)]
    public async Task GivenValidTradeRequest_WhenPostEndpointCalledWithoutCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
    {
        var tradeRequest = new SaveTradeRequest(tickerSymbol, price, count, brokerId);
        var httpResponse = await PostTradeAsync(tradeRequest, apiRoute);
        var tradeResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        AssertReturnsNewCorrelationId(httpResponse);
    }

    [Theory]
    [InlineData("012345678901234567891", 10, 1, "BR10834", _apiRoute)]
    [InlineData("012345678901234567891", 10, 1, "BR10834", _apiRouteV1)]
    [InlineData("012345678901234567891", 10, 1, "BR10834", _apiRouteV2)]
    public async Task GivenValidTradeRequestWithTickerSymbolOver20Chars_WhenPostEndpointCalled_ThenDoesNotAddTradeAndReturnsBadRequest(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
            => await PostTradeAndAssertNotSavedAndBadRequest(tickerSymbol, price, count, brokerId, apiRoute);

    [Theory]
    [InlineData("", 10, 1, "BR10834", _apiRoute)]
    [InlineData(" ", 10, 1, "BR10834", _apiRoute)]
    [InlineData(null, 10, 1, "BR10834", _apiRoute)]
    [InlineData("", 10, 1, "BR10834", _apiRouteV1)]
    [InlineData(" ", 10, 1, "BR10834", _apiRouteV1)]
    [InlineData(null, 10, 1, "BR10834", _apiRouteV1)]
    public async Task GivenValidTradeRequestWithEmptyTickerSymbol_WhenPostEndpointCalled_ThenDoesNotAddTradeAndReturnsBadRequest(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
            => await PostTradeAndAssertNotSavedAndBadRequest(tickerSymbol, price, count, brokerId, apiRoute);

    [Theory]
    [InlineData("NASDAQ:AAPL", 0, 1, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:AAPL", -1, 1, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:AAPL", 0, 1, "BR10900", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", -1, 1, "BR10900", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", 0, 1, "BR10900", _apiRouteV2)]
    [InlineData("NASDAQ:AAPL", -1, 1, "BR10900", _apiRouteV2)]
    public async Task GivenValidTradeRequestWithPriceZeroOrLess_WhenPostEndpointCalled_ThenDoesNotAddTradeAndReturnsBadRequest(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
            => await PostTradeAndAssertNotSavedAndBadRequest(tickerSymbol, price, count, brokerId, apiRoute);

    [Theory]
    [InlineData("NASDAQ:AAPL", 150, 0, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:AAPL", 200, -1, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:AAPL", 300, 0, "BR10834", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", 400, -1, "BR10834", _apiRouteV1)]
    [InlineData("NASDAQ:AAPL", 300, 0, "BR10834", _apiRouteV2)]
    [InlineData("NASDAQ:AAPL", 400, -1, "BR10834", _apiRouteV2)]
    public async Task GivenValidTradeRequestWithCountZeroOrLess_WhenPostEndpointCalled_ThenDoesNotAddTradeAndReturnsBadRequest(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
            => await PostTradeAndAssertNotSavedAndBadRequest(tickerSymbol, price, count, brokerId, apiRoute);

    [Theory]
    [InlineData("NASDAQ:ERROR", 150, 4, "BR10834", _apiRoute)]
    [InlineData("NASDAQ:ERROR", 300, 4, "CR10834", _apiRouteV1)]
    [InlineData("NASDAQ:ERROR", 300, 4, "DR10834", _apiRouteV2)]
    public async Task GivenValidTradeRequest_WhenPostEndpointCalledAndAnErrorOccurs_ThenDoesAddNotTradeAndReturnsInternalServerError(
        string tickerSymbol, decimal price, decimal count, string brokerId, string apiRoute)
    {
        var saveTradeRequest = new SaveTradeRequest(tickerSymbol, price, count, brokerId);
        var response = await PostTradeAsync(saveTradeRequest, apiRoute);

        AssertTradeSaved(saveTradeRequest);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PostTradeAsync(SaveTradeRequest tradeRequest, string apiRoute)
            => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(tradeRequest));

    private static StringContent BuildHttpContent(SaveTradeRequest tradeRequest)
        => new (JsonSerializer.Serialize(tradeRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private void AssertTradeSaved(SaveTradeRequest tradeRequest)
            => _context.MockTradeRepository.Verify(m => m.SaveTradeAsync(MapToTrade(tradeRequest)),
                Times.Once);

    private static Trade MapToTrade(SaveTradeRequest saveTradeRequest)
        => new (saveTradeRequest.TickerSymbol, saveTradeRequest.Price,
            saveTradeRequest.Count, saveTradeRequest.BrokerId);

    private void AssertTradeNotSaved(SaveTradeRequest tradeRequest)
            => _context.MockTradeRepository.Verify(m => m.SaveTradeAsync(MapToTrade(tradeRequest)),
                Times.Never);

    private async Task PostTradeAndAssertNotSavedAndBadRequest(string tickerSymbol, decimal price, decimal count, string brokerId,
        string apiRoute)
    {
        var tradeRequest = new SaveTradeRequest(tickerSymbol, price, count, brokerId);
        var response = await PostTradeAsync(tradeRequest, apiRoute);

        AssertTradeNotSaved(tradeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<SaveTradeResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SaveTradeResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertTradeResponseEqualToTradeRequest(SaveTradeRequest tradeRequest, SaveTradeResponse? tradeResponse)
    {
        Assert.Equal(tradeRequest.TickerSymbol, tradeResponse?.TickerSymbol);
        Assert.Equal(tradeRequest.Price, tradeResponse?.Price);
        Assert.Equal(tradeRequest.Count, tradeResponse?.Count);
        Assert.Equal(tradeRequest.BrokerId, tradeResponse?.BrokerId);
    }

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("X-Correlation-Id", out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }
}
