using LSE.Stocks.Api.Models;
using LSE.Stocks.Application.Services.Shares.Queries.GetSharePrice;
using LSE.Stocks.Domain.Models.Shares;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LSE.Stocks.Api.Controllers.V2;

[ApiVersion("2.0")]
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
    [Route("{tickerSymbol}")]
    public async Task<ActionResult<SharePriceResponse>> GetPrice(string tickerSymbol)
    {
        var sharePriceQueryResponse = await _mediator.Send(new GetSharePriceQuery(tickerSymbol));

        return new OkObjectResult(BuildSharePriceQueryResponse(sharePriceQueryResponse.SharePrice));
    }

    private static SharePriceResponse BuildSharePriceQueryResponse(SharePrice sharePrice)
        => new(sharePrice.TickerSymbol, sharePrice.Price);
}
