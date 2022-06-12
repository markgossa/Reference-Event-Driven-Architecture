using MediatR;

namespace LSE.Stocks.Application.Services.Shares.Queries.GetSharePrice;

public record GetSharePriceQuery(string TickerSymbol) : IRequest<GetSharePriceQueryResponse>;
