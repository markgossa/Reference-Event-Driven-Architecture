#nullable disable

namespace LSE.Stocks.Infrastructure.Models;

public class TradeRow
{
    public int Id { get; set; }
    public string TickerSymbol { get; set; }
    public decimal Price { get; set; }
    public decimal Count { get; set; }
    public string BrokerId { get; set; }
}
