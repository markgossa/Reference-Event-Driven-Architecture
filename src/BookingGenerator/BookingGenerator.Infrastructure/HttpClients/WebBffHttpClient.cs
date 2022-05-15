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

    public async Task PostAsync(Booking booking, string correlationId)
    {
        var request = BuildRequest(booking, correlationId);
        await SendRequestAsync(request);
    }

    private HttpRequestMessage BuildRequest(Booking booking, string correlationId)
    {
        var webBffBookingRequest = MapToWebBffBookingRequest(booking);
        
        return BuildHttpRequest(webBffBookingRequest, correlationId);
    }
    
    private async Task SendRequestAsync(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static WebBffBookingRequest MapToWebBffBookingRequest(Booking booking)
        => new(booking.FirstName, booking.LastName, booking.StartDate.ToDateTime(default),
            booking.EndDate.ToDateTime(default), booking.Destination, booking.Price);

    private static StringContent BuildHttpContent(WebBffBookingRequest webBffBookingRequest) 
        => new(JsonSerializer.Serialize(webBffBookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private HttpRequestMessage BuildHttpRequest(WebBffBookingRequest webBffBookingRequest, string correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _settings.WebBffBookingsUrl)
        {
            Content = BuildHttpContent(webBffBookingRequest)
        };

        request.Headers.Add("X-Correlation-Id", correlationId);

        return request;
    }
}
