using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using BookingGenerator.Infrastructure.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class WebBffHttpClientTests
{
    private readonly string _webBffUrl = "https://webbffurl";

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff()
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking();
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId, booking));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        var response = await webBffHttpClient.PostAsync(booking, correlationId);

        Assert.Equal(statusCode, response.StatusCode);
    }

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncWithCorrelationId_PostsRequestToWebBffWithSameCorrelationId()
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking();
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId, booking));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        var response = await webBffHttpClient.PostAsync(booking, correlationId);

        Assert.Equal(statusCode, response.StatusCode);
    }

    private IOptions<WebBffHttpClientSettings> BuildMockOptions()
    {
        var mockOptions = new Mock<IOptions<WebBffHttpClientSettings>>();
        mockOptions.Setup(m => m.Value).Returns(new WebBffHttpClientSettings
        {
            InitialRetryIntervalMilliseconds = 1,
            MaxAttempts = 1,
            WebBffBookingsUrl = _webBffUrl
        });

        return mockOptions.Object;
    }

    private HttpMessageHandler BuildMockMessageHandler(HttpStatusCode statusCode, string correlationId, Booking booking)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => IsExpectedHttpRequestMessage(correlationId, r, booking)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return mockMessageHandler.Object;
    }

    private bool IsExpectedHttpRequestMessage(string correlationId, HttpRequestMessage request, Booking booking)
        => request.RequestUri == new Uri(_webBffUrl)
            && request.Headers.Single(h => h.Key == "X-Correlation-Id").Value.Single() == correlationId
            && IsExpectedWebBffBookingRequest(request, booking);

    private static bool IsExpectedWebBffBookingRequest(HttpRequestMessage request, Booking booking)
    {
        var webBffBookingRequest = DeserializeRequest(request);

        return webBffBookingRequest?.BookingId == booking.BookingId
            && webBffBookingRequest?.BookingSummary?.FirstName == booking.BookingSummary.FirstName
            && webBffBookingRequest?.BookingSummary?.LastName == booking.BookingSummary.LastName
            && webBffBookingRequest?.BookingSummary?.StartDate == booking.BookingSummary.StartDate
            && webBffBookingRequest?.BookingSummary?.EndDate == booking.BookingSummary.EndDate
            && webBffBookingRequest?.BookingSummary?.Destination == booking.BookingSummary.Destination
            && webBffBookingRequest?.BookingSummary?.Price == booking.BookingSummary.Price;
    }

    private static WebBffBookingRequest? DeserializeRequest(HttpRequestMessage request)
    {
        var json = request.Content?.ReadAsStringAsync().Result;

        return JsonSerializer.Deserialize<WebBffBookingRequest>(json ?? string.Empty);
    }

    private static Booking BuildNewBooking()
    {
        var randomString = Guid.NewGuid().ToString();
        var randomNumber = new Random().Next(1, 1000);
        var randomDate = DateTime.UtcNow.AddDays(randomNumber);
        var randomBool = randomNumber % 2 == 0;
        var randomCarSize = GetRandomEnum<Domain.Enums.Size>();
        var randomCarTransmission = GetRandomEnum<Domain.Enums.Transmission>();

        var bookingSummary = new BookingSummary(randomString, randomString, randomDate, randomDate, randomString, randomNumber);
        var car = new CarBooking(randomString, randomCarSize, randomCarTransmission);
        var hotel = new HotelBooking(randomNumber, randomBool, randomBool, randomBool);
        var flight = new FlightBooking(randomDate, randomString, randomDate, randomString);

        return new Booking(randomString, bookingSummary, car, hotel, flight);
    }

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }
}
