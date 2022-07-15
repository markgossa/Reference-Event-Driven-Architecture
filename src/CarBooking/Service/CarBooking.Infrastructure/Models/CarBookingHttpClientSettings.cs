#nullable disable

namespace CarBooking.Infrastructure.Models;
public class CarBookingHttpClientSettings
{
    public string BaseUri { get; set; }
    public int MaxAttempts { get; set; }
    public int InitialRetryIntervalMilliseconds { get; set; }
}
