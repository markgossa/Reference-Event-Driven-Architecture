#nullable disable

namespace WebBff.Infrastructure.HttpClients;
public class WebBffHttpClientSettings
{
    public string WebBffBookingsUrl { get; set; }
    public int MaxAttempts { get; set; }
    public int InitialRetryIntervalMilliseconds { get; set; }
}
