using LSE.Stocks.Api.Models;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace LSE.Stocks.Api.Tests.Component.V1;

public class GetSharePriceV2Tests : IClassFixture<ApiTestsContext>
{
    private const string _apiRoute = "v2/shareprices";
    private readonly ApiTestsContext _context;

    public GetSharePriceV2Tests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("NASDAQ:TSLA", 250, _apiRoute)]
    public async Task GivenValidSharePriceRequest_WhenGetEndpointCalled_ThenReturnsAveragePriceAndReturnsOK(
        string tickerSymbol, decimal expectedPrice, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);
        var price = await DeserializeResponseAsync(response);

        Assert.Equal(expectedPrice, price?.Price);
        Assert.Equal(tickerSymbol, price?.TickerSymbol);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("NASDAQ:AAPL", _apiRoute)]
    public async Task GivenValidSharePriceRequest_WhenGetEndpointCalledWithNoCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
        string tickerSymbol, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);
        var price = await DeserializeResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertReturnsNewCorrelationId(response);
    }

    [Theory]
    [InlineData("NOTFOUND", _apiRoute)]
    public async Task GivenSharePriceRequestForInvalidShare_WhenGetEndpointCalled_ThenReturnsNotFound(
        string tickerSymbol, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("012345678901234567891", _apiRoute)]
    public async Task GivenSharePriceRequestForTickerSymbolOver20Chars_WhenGetEndpointCalled_ThenReturnsBadRequest(
        string tickerSymbol, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", _apiRoute)]
    [InlineData(" ", _apiRoute)]
    [InlineData(null, _apiRoute)]
    public async Task GivenSharePriceRequestForTickerSymbolEmpty_WhenGetEndpointCalled_ThenReturnsBadRequest(
        string tickerSymbol, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Theory]
    [InlineData("NASDAQ:ERROR", _apiRoute)]
    public async Task GivenSharePriceReositoryHasError_WhenGetEndpointCalled_ThenReturnsInternalServerError(
        string tickerSymbol, string apiRoute)
    {
        var response = await GetSharePriceAsync(tickerSymbol, apiRoute);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private static async Task<SharePriceResponse?> DeserializeResponseAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<SharePriceResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<HttpResponseMessage> GetSharePriceAsync(string tickerSymbol, string apiRoute)
            => await _context.HttpClient.GetAsync($"{apiRoute}/{tickerSymbol}");

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("X-Correlation-Id", out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }
}
