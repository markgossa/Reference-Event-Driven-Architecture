namespace LSE.Stocks.Api.Models;

public record SaveTradeResponse(string TickerSymbol, decimal Price, decimal Count, string BrokerId);
