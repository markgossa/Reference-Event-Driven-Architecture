using MediatR;

namespace LSE.Stocks.Application.Services.Shares.Commands.SaveTrade;

public record SaveTradeCommand(string TickerSymbol, decimal Price, decimal Count, string BrokerId) : IRequest;
