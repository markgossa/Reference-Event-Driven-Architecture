using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using BookingGenerator.Infrastructure.Models;
using Common.Messaging.CorrelationIdGenerator;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceTests
{
    private readonly string _webBffUrl = "https://webbffurl";

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId, booking));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        await MakeNewBookingAsync(webBffHttpClient, SetUpMockCorrelationIdGenerator(correlationId),
            booking);
    }

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncWithCorrelationId_PostsRequestToWebBffWithSameCorrelationId(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId, booking));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        await MakeNewBookingWithCorrelationIdAsync(correlationId, webBffHttpClient, booking);
    }

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncAndThereIsAnError_ThrowsException(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId, booking));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        await Assert.ThrowsAsync<HttpRequestException>(async ()
            => await MakeNewBookingAsync(webBffHttpClient, SetUpMockCorrelationIdGenerator(correlationId), booking));
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

        return webBffBookingRequest?.FirstName == booking.FirstName
            && webBffBookingRequest?.LastName == booking.LastName
            && webBffBookingRequest.StartDate == booking.StartDate.ToDateTime(default)
            && webBffBookingRequest.EndDate == booking.EndDate.ToDateTime(default)
            && webBffBookingRequest.Destination == booking.Destination
            && webBffBookingRequest.Price == booking.Price;

    }

    private static WebBffBookingRequest? DeserializeRequest(HttpRequestMessage request)
    {
        var json = request.Content?.ReadAsStringAsync().Result;
        
        return JsonSerializer.Deserialize<WebBffBookingRequest>(json ?? string.Empty);
    }

    private static async Task MakeNewBookingAsync(WebBffHttpClient webBffHttpClient,
        ICorrelationIdGenerator mockCorrelationIdGenerator, Booking booking)
            => await new BookingService(webBffHttpClient, mockCorrelationIdGenerator)
                .BookAsync(booking);

    private static Booking BuildNewBooking(string firstName, string lastName, string startDate, string endDate,
        string destination, decimal price)
            => new(firstName, lastName, DateOnly.Parse(startDate), DateOnly.Parse(endDate), destination, price);

    private static ICorrelationIdGenerator SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.CorrelationId).Returns(correlationId);

        return mockCorrelationIdGenerator.Object;
    }

    private static async Task MakeNewBookingWithCorrelationIdAsync(string correlationId,
        WebBffHttpClient webBffHttpClient, Booking booking)
            => await new BookingService(webBffHttpClient, new CorrelationIdGenerator())
                .BookAsync(booking, correlationId);
}
