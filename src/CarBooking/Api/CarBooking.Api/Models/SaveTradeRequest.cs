namespace LSE.Stocks.Api.Models;

public record SaveTradeRequest(string TickerSymbol, decimal Price, decimal Count, string BrokerId);
