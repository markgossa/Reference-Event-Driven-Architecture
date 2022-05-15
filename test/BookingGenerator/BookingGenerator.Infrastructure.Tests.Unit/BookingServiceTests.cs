using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using Common.Messaging.CorrelationIdGenerator;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceTests
{
    private readonly string _webBffUrl = "https://webbffurl";

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff()
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        await MakeNewBookingAsync(webBffHttpClient, SetUpMockCorrelationIdGenerator(correlationId));
    }
    
    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncWithCorrelationId_PostsRequestToWebBffWithSameCorrelationId()
    {
        var statusCode = HttpStatusCode.Accepted;
        var correlationId = Guid.NewGuid().ToString();
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());
        
        await MakeNewBookingWithCorrelationIdAsync(correlationId, webBffHttpClient);
    }

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncAndThereIsAnError_ThrowsException()
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var correlationId = Guid.NewGuid().ToString();
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode, correlationId));
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions());

        await Assert.ThrowsAsync<HttpRequestException>(async () 
            => await MakeNewBookingAsync(webBffHttpClient, SetUpMockCorrelationIdGenerator(correlationId)));
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

    private HttpMessageHandler BuildMockMessageHandler(HttpStatusCode statusCode, string correlationId)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri == new Uri(_webBffUrl)
                    && r.Headers.Single(h => h.Key == "X-Correlation-Id").Value.Single() == correlationId),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return mockMessageHandler.Object;
    }

    private static async Task MakeNewBookingAsync(WebBffHttpClient webBffHttpClient, 
        ICorrelationIdGenerator mockCorrelationIdGenerator)
            => await new BookingService(webBffHttpClient, mockCorrelationIdGenerator)
                .BookAsync(new Booking("Joe", "Bloggs", DateOnly.Parse("15/05/2022"), 
                    DateOnly.Parse("22/05/2022"), "Jamaica", 799));

    private static ICorrelationIdGenerator SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.CorrelationId).Returns(correlationId);

        return mockCorrelationIdGenerator.Object;
    }

    private static async Task MakeNewBookingWithCorrelationIdAsync(string correlationId, WebBffHttpClient webBffHttpClient)
        => await new BookingService(webBffHttpClient, new CorrelationIdGenerator())
            .BookAsync(new Booking("Joe", "Bloggs", DateOnly.Parse("15/05/2022"),
                DateOnly.Parse("22/05/2022"), "Jamaica", 799), correlationId);
}