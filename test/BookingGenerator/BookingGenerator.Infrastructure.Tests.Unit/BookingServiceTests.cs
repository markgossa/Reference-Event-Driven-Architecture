using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceTests
{
    private readonly string _webBffUrl = "https://webbffurl";

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff()
    {
        var statusCode = HttpStatusCode.Accepted;
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode).Object);
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions().Object);

        await MakeNewBookingAsync(webBffHttpClient);
    }

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncAndThereIsAnError_ThrowsException()
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var httpClient = new HttpClient(BuildMockMessageHandler(statusCode).Object);
        var webBffHttpClient = new WebBffHttpClient(httpClient, BuildMockOptions().Object);

        await Assert.ThrowsAsync<HttpRequestException>(async () => await MakeNewBookingAsync(webBffHttpClient));
    }

    private Mock<IOptions<WebBffHttpClientSettings>> BuildMockOptions()
    {
        var mockOptions = new Mock<IOptions<WebBffHttpClientSettings>>();
        mockOptions.Setup(m => m.Value).Returns(new WebBffHttpClientSettings
        {
            InitialRetryIntervalMilliseconds = 1,
            MaxAttempts = 1,
            WebBffBookingsUrl = _webBffUrl
        });

        return mockOptions;
    }

    private Mock<HttpMessageHandler> BuildMockMessageHandler(HttpStatusCode statusCode)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri == new Uri(_webBffUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return mockMessageHandler;
    }

    private static async Task MakeNewBookingAsync(WebBffHttpClient webBffHttpClient) 
        => await new BookingService(webBffHttpClient).BookAsync(new Booking("Joe", "Bloggs", 
            DateOnly.Parse("15/05/2022"), DateOnly.Parse("22/05/2022"), "Jamaica", 799));
}