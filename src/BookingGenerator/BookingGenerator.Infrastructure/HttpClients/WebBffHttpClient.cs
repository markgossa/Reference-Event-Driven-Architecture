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

    public async Task<HttpResponseMessage> PostAsync(Booking booking, string correlationId)
    {
        var request = BuildRequest(booking, correlationId);
        return await SendRequestAsync(request);
    }

    private HttpRequestMessage BuildRequest(Booking booking, string correlationId)
    {
        var webBffBooking = MapToWebBffBooking(booking);
        
        return BuildHttpRequest(webBffBooking, correlationId);
    }
    
    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return response;
    }

    private static WebBffBookingRequest MapToWebBffBooking(Booking booking)
        => new(booking.BookingId, MapToBookingSummary(booking), MapToCarBooking(booking), 
            MapToHotelBooking(booking), MapToFlightBooking(booking));

    private static WebBffFlightBookingRequest MapToFlightBooking(Booking booking)
        => new(booking.FlightBooking.OutboundFlightTime,
            booking.FlightBooking.OutboundFlightNumber, booking.FlightBooking.InboundFlightTime,
            booking.FlightBooking.InboundFlightNumber);

    private static WebBffHotelBookingRequest MapToHotelBooking(Booking booking)
        => new(booking.HotelBooking.NumberOfBeds, booking.HotelBooking.BreakfastIncluded,
            booking.HotelBooking.LunchIncluded, booking.HotelBooking.DinnerIncluded);

    private static WebBffCarBookingRequest MapToCarBooking(Booking booking)
        => new(booking.CarBooking.PickUpLocation,
            MapToAnotherEnum<Enums.Size>(booking.CarBooking.Size.ToString()),
            MapToAnotherEnum<Enums.Transmission>(booking.CarBooking.Transmission.ToString()));

    private static WebBffBookingSummaryRequest MapToBookingSummary(Booking booking)
        => new(booking.BookingSummary.FirstName,
            booking.BookingSummary.LastName, booking.BookingSummary.StartDate,
            booking.BookingSummary.EndDate, booking.BookingSummary.Destination,
            booking.BookingSummary.Price);

    private static StringContent BuildHttpContent(WebBffBookingRequest webBffBooking) 
        => new(JsonSerializer.Serialize(webBffBooking), Encoding.UTF8, MediaTypeNames.Application.Json);

    private HttpRequestMessage BuildHttpRequest(WebBffBookingRequest webBffBooking, string correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _settings.WebBffBookingsUrl)
        {
            Content = BuildHttpContent(webBffBooking)
        };

        request.Headers.Add("X-Correlation-Id", correlationId);

        return request;
    }

    private static T MapToAnotherEnum<T>(string value) where T : struct
        => Enum.Parse<T>(value);
}
