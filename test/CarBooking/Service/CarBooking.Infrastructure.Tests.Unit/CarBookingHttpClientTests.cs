using CarBooking.Infrastructure.Enums;
using CarBooking.Infrastructure.Clients;
using CarBooking.Infrastructure.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace CarBooking.Infrastructure.Tests.Unit;
public class CarBookingHttpClientTests
{
    private readonly Random _random = new();
    private readonly string _carBookingApiUrl = $"https://{Guid.NewGuid()}";
    private readonly Mock<IOptions<CarBookingHttpClientSettings>> _mockOptions = new();

    public CarBookingHttpClientTests() => SetUpMockOptions();

    [Fact]
    public async void GivenANewInstance_WhenICallPostAsync_ThenSubmitsDataToCarBookingApi()
    {
        var carBookingRequest = BuildNewCarBookingRequest();
        var mockMessageHandler = BuildMockMessageHandler(HttpStatusCode.OK,
            carBookingRequest.BookingId, carBookingRequest);

        var sut = new CarBookingHttpClient(new HttpClient(mockMessageHandler.Object), _mockOptions.Object);
        await sut.PostAsync(carBookingRequest);

        AssertCarBookingRequestSent(carBookingRequest, mockMessageHandler);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    public async void GivenANewInstance_WhenICallPostAsyncAndThereIsAnErrorOrConflictOrBadRequest_ThenDoesNotThrowHttpRequestException(
        HttpStatusCode httpStatusCode)
    {
        var carBookingRequest = BuildNewCarBookingRequest();
        var mockMessageHandler = BuildMockMessageHandler(httpStatusCode, carBookingRequest.BookingId, carBookingRequest);

        var sut = new CarBookingHttpClient(new HttpClient(mockMessageHandler.Object), _mockOptions.Object);
        await sut.PostAsync(carBookingRequest);
    }

    private CarBookingRequest BuildNewCarBookingRequest()
        => new(GetRandomString(), GetRandomString(), GetRandomString(),
            GetRandomDate(), GetRandomDate(), GetRandomString(), GetRandomNumber(),
            GetRandomEnum<Size>(), GetRandomEnum<Transmission>());

    private DateTime GetRandomDate() => DateTime.Now.AddDays(GetRandomNumber());

    private int GetRandomNumber() => _random.Next(1000, 9000);

    private static string GetRandomString() => Guid.NewGuid().ToString();

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    private Mock<HttpMessageHandler> BuildMockMessageHandler(HttpStatusCode statusCode, string correlationId,
        CarBookingRequest carBookingRequest)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => IsExpectedHttpRequestMessage(correlationId, r, carBookingRequest)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return mockMessageHandler;
    }

    private bool IsExpectedHttpRequestMessage(string correlationId, HttpRequestMessage request, CarBookingRequest carBooking)
        => request.RequestUri == new Uri(_carBookingApiUrl)
            && IsExpectedCorrelationIdHeader(correlationId, request)
            && request.Method == HttpMethod.Post
            && IsExpectedCarBookingRequest(request, carBooking);
    
    private static bool IsExpectedCorrelationIdHeader(string correlationId, HttpRequestMessage request) 
        => request.Headers.Single(h => h.Key == "X-Correlation-Id").Value.Single() == correlationId;

    private static bool IsExpectedCarBookingRequest(HttpRequestMessage request, CarBookingRequest expectedCarBookingRequest)
    {
        var actualCarBookingRequest = DeserializeRequest(request);

        return actualCarBookingRequest?.BookingId == expectedCarBookingRequest.BookingId
            && actualCarBookingRequest?.FirstName == expectedCarBookingRequest.FirstName
            && actualCarBookingRequest?.LastName == expectedCarBookingRequest.LastName
            && actualCarBookingRequest?.StartDate == expectedCarBookingRequest.StartDate
            && actualCarBookingRequest?.EndDate == expectedCarBookingRequest.EndDate
            && actualCarBookingRequest?.PickUpLocation == expectedCarBookingRequest.PickUpLocation
            && actualCarBookingRequest?.Price == expectedCarBookingRequest.Price
            && actualCarBookingRequest?.Size == expectedCarBookingRequest.Size
            && actualCarBookingRequest?.Transmission == expectedCarBookingRequest.Transmission;
    }

    private static CarBookingRequest? DeserializeRequest(HttpRequestMessage request)
    {
        var json = request.Content?.ReadAsStringAsync().Result;

        return JsonSerializer.Deserialize<CarBookingRequest>(json ?? string.Empty);
    }

    private void AssertCarBookingRequestSent(CarBookingRequest carBookingRequest,
        Mock<HttpMessageHandler> mockMessageHandler)
        => mockMessageHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => IsExpectedHttpRequestMessage(
                carBookingRequest.BookingId, r, carBookingRequest)
                && r.RequestUri == new Uri(_carBookingApiUrl)),
            ItExpr.IsAny<CancellationToken>());

    private void SetUpMockOptions() => _mockOptions.Setup(m => m.Value).Returns(new CarBookingHttpClientSettings
    {
        BaseUri = _carBookingApiUrl
    });
}
