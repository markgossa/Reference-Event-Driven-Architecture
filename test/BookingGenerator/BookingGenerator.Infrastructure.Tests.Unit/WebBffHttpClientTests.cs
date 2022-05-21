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

        await webBffHttpClient.PostAsync(booking, correlationId);
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

        await webBffHttpClient.PostAsync(booking, correlationId);
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
            => await webBffHttpClient.PostAsync(booking, correlationId));
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
            && webBffBookingRequest.StartDate == booking.StartDate
            && webBffBookingRequest.EndDate == booking.EndDate
            && webBffBookingRequest.Destination == booking.Destination
            && webBffBookingRequest.Price == booking.Price;
    }

    private static WebBffBookingRequest? DeserializeRequest(HttpRequestMessage request)
    {
        var json = request.Content?.ReadAsStringAsync().Result;

        return JsonSerializer.Deserialize<WebBffBookingRequest>(json ?? string.Empty);
    }

    private static Booking BuildNewBooking(string firstName, string lastName, string startDate, string endDate,
        string destination, decimal price)
            => new(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate), destination, price);
}
