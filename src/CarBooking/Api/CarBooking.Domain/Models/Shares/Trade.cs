namespace LSE.Stocks.Domain.Models.Shares;

public record Trade(string TickerSymbol, decimal Price, decimal Count, string? BrokerId);
