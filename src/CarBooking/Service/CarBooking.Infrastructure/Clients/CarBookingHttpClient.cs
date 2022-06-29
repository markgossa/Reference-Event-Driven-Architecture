using CarBooking.Infrastructure.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace CarBooking.Infrastructure.Clients;
public class CarBookingHttpClient : ICarBookingHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<CarBookingHttpClientSettings> _options;

    public CarBookingHttpClient(HttpClient httpClient,
        IOptions<CarBookingHttpClientSettings> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task PostAsync(CarBookingRequest carBookingRequest)
    {
        var httpRequest = BuildHttpRequestMessage(carBookingRequest);

        AddCorrelationIdHeader(carBookingRequest, httpRequest);
        await SendRequestAsync(httpRequest);
    }

    private async Task SendRequestAsync(HttpRequestMessage httpRequest)
    {
        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage BuildHttpRequestMessage(CarBookingRequest carBookingRequest)
        => new()
        {
            Content = BuildHttpContent(carBookingRequest),
            RequestUri = new Uri(_options.Value.BaseUri),
            Method = HttpMethod.Post
        };

    private static void AddCorrelationIdHeader(CarBookingRequest carBookingRequest, HttpRequestMessage httpRequest)
    {
        const string correlationIdHeaderName = "X-Correlation-Id";
        httpRequest.Headers.Add(correlationIdHeaderName, carBookingRequest.BookingId);
    }

    private static StringContent BuildHttpContent(CarBookingRequest carBookingRequest)
        => new(JsonSerializer.Serialize(carBookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);
}
