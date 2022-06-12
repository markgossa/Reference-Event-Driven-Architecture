using LSE.Stocks.Api.Models;
using LSE.Stocks.Application.Services.Shares.Queries.GetSharePrice;
using LSE.Stocks.Domain.Models.Shares;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LSE.Stocks.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("SharePrices")]
[Route("v{version:apiVersion}/SharePrices")]
[ApiController]
public class SharePricesController : Controller
{
    private readonly IMediator _mediator;

    public SharePricesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Gets the price for a ticker symbol
    /// </summary>
    /// <param name="tickerSymbol"></param>
    /// <returns>A SharePriceResponse which contains the price of the share</returns>
    /// <response code="200">Returns 200 and the share price</response>
    /// <response code="400">Returns 400 if the query is invalid</response>
    [HttpGet]
    public async Task<ActionResult<SharePriceResponse>> GetPrice([FromQuery] string tickerSymbol)
    {
        var sharePrice = await GetSharePriceAsync(tickerSymbol);

        return new OkObjectResult(new SharePriceResponse(sharePrice.TickerSymbol, sharePrice.Price));
    }

    private async Task<SharePrice> GetSharePriceAsync(string tickerSymbol)
        => (await _mediator.Send(new GetSharePriceQuery(tickerSymbol))).SharePrice;
}
