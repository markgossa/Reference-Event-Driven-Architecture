using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using WebBff.Api.Models;
using WebBff.Domain.Models;
using Xunit;

namespace WebBff.Tests.Component;

public class WebBffApiTests : IClassFixture<ApiTestsContext>
{
    private const string _apiRoute = "bookings";
    private const string _apiRouteV1 = "v1/bookings";
    protected const string _errorFirstName = "Unlucky";
    private readonly ApiTestsContext _context;

    public WebBffApiTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Malta", 100.43, _apiRoute)]
    [InlineData("Mary", "Bloggs", 0, 5, "Malta", 100.43, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Corfu", 100.43, _apiRouteV1)]
    [InlineData("Emilia", "Smith", 10, 15, "Corfu", 100.43, _apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalled_ThenPublishesBookingCreatedMessageAndReturnsAccepted(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, string apiRoute)
    {
        var bookingRequest = BuildBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price);

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);
        var bookingResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertBookingResponseEqualToBookingRequest(bookingRequest, bookingResponse);
        AssertBookingCreatedMessageSent(bookingRequest);
    }

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Greece", 100.43, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Nice", 100.43, _apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalledWithoutCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, string apiRoute)
    {
        var bookingRequest = BuildBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price);

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);
        await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertReturnsNewCorrelationId(httpResponse);
        AssertBookingCreatedMessageSent(bookingRequest);
    }

    [Theory]
    [InlineData(_errorFirstName, "Bloggs", 0, 5, "Greece", 100.43, _apiRoute)]
    [InlineData(_errorFirstName, "Smith", 10, 15, "Nice", 100.43, _apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalledAndThereIsAnError_ThenReturnsInternalServerError(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, string apiRoute)
    {
        var bookingRequest = BuildBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price);

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> MakeBookingAsync(BookingRequest bookingRequest, string apiRoute)
            => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(bookingRequest));

    private static StringContent BuildHttpContent(BookingRequest bookingRequest)
        => new(JsonSerializer.Serialize(bookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private static async Task<BookingResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<BookingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertBookingResponseEqualToBookingRequest(BookingRequest bookingRequest,
        BookingResponse? bookingResponse)
    {
        Assert.Equal(bookingResponse?.FirstName, bookingRequest?.FirstName);
        Assert.Equal(bookingResponse?.LastName, bookingRequest?.LastName);
        Assert.Equal(bookingResponse?.StartDate, bookingRequest?.StartDate);
        Assert.Equal(bookingResponse?.EndDate, bookingRequest?.EndDate);
        Assert.Equal(bookingResponse?.Destination, bookingRequest?.Destination);
        Assert.Equal(bookingResponse?.Price, bookingRequest?.Price);
    }

    private static BookingRequest BuildBookingRequest(string firstName, string lastName, int daysTillStartDate,
        int daysTillEndDate, string destination, decimal price)
            => new(firstName, lastName, DateTime.UtcNow.AddDays(daysTillStartDate),
                DateTime.UtcNow.AddDays(daysTillEndDate), destination, price);

    private static Booking BuildBooking(BookingRequest bookingRequest)
        => new(bookingRequest.FirstName, bookingRequest.LastName,
            bookingRequest.StartDate, bookingRequest.EndDate, bookingRequest.Destination,
            bookingRequest.Price);

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("X-Correlation-Id", out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }

    private void AssertBookingCreatedMessageSent(BookingRequest bookingRequest)
        => _context.MockBookingRepository.Verify(m => m.SendBookingAsync(BuildBooking(bookingRequest)), Times.Once);
}
