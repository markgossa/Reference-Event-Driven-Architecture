using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace BookingGenerator.Infrastructure.HttpClients;
public class WebBffHttpClient : IWebBffHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly WebBffHttpClientSettings _settings;

    public WebBffHttpClient(HttpClient httpClient, IOptions<WebBffHttpClientSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task PostAsync(Booking booking)
    {
        var webBffBookingRequest = MapToWebBffBookingRequest(booking);

        var json = JsonSerializer.Serialize(webBffBookingRequest);
        using var httpContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var response = await _httpClient.PostAsync(_settings.WebBffBookingsUrl, httpContent);
        response.EnsureSuccessStatusCode();
    }

    private static WebBffBookingRequest MapToWebBffBookingRequest(Booking booking) 
        => new(booking.FirstName, booking.LastName, booking.StartDate,
                booking.EndDate, booking.Destination, booking.Price);
}
