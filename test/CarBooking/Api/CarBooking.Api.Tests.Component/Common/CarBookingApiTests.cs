using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CarBooking.Api.Tests.Component.Common;

public class CarBookingApiTests : IClassFixture<ApiTestsContext>
{
    private const string _apiRoute = "bookings";
    private const string _apiRouteV1 = "v1/bookings";
    private const string _correlationIdHeader = "X-Correlation-Id";
    private readonly ApiTestsContext _context;

    public CarBookingApiTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Malta", 100.43, _apiRoute)]
    [InlineData("Mary", "Bloggs", 0, 5, "Malta", 100.43, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Corfu", 100.43, _apiRouteV1)]
    [InlineData("Emilia", "Smith", 10, 15, "Corfu", 100.43, _apiRouteV1)]
    public async Task GivenValidCarBookingRequest_WhenPostEndpointCalled_ThenSubmitsBookingsToBffAndReturnsAccepted(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, string apiRoute)
    {
        var carBookingRequest = BuildCarBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price);

        var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);
        var bookingResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertCarBookingRequestsSentToBff(carBookingRequest);
        AssertBookingResponseEqualToCarBookingRequest(carBookingRequest, bookingResponse);
    }

    //[Theory]
    //[InlineData("Joe", "Bloggs", 0, 5, "Greece", 100.43, _apiRoute)]
    //[InlineData("John", "Smith", 10, 15, "Nice", 100.43, _apiRouteV1)]
    //public async Task GivenValidCarBookingRequest_WhenPostEndpointCalledWithoutCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
    //    string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
    //    decimal price, string apiRoute)
    //{
    //    var carBookingRequest = BuildCarBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
    //        destination, price);

    //    var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);
    //    await DeserializeResponse(httpResponse);

    //    Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
    //    AssertReturnsNewCorrelationId(httpResponse);
    //}

    //[Theory]
    //[InlineData("Unlucky", "Bloggs", 0, 5, "Greece", 100.43, _apiRoute)]
    //[InlineData("Unlucky", "Smith", 10, 15, "Nice", 100.43, _apiRouteV1)]
    //public async Task GivenValidCarBookingRequest_WhenPostEndpointCalledAndThereIsAnError_ThenReturnsInternalServerError(
    //    string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
    //    decimal price, string apiRoute)
    //{
    //    var carBookingRequest = BuildCarBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
    //        destination, price);

    //    var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);

    //    Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
    //}

    //[Fact]
    //public async Task GivenValidCarBookingRequest_WhenPostEndpointCalledWithDuplicateCorrelationId_ThenReturnsConflict()
    //{
    //    var carBookingRequest = BuildCarBookingRequest("Duplicate", "Bloggs", 5, 29, "Belize", 1700);

    //    var httpResponse = await MakeBookingAsync(carBookingRequest, _apiRoute);

    //    Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    //}

    private async Task<HttpResponseMessage> MakeBookingAsync(CarBookingRequest carBookingRequest, string apiRoute)
            => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(carBookingRequest));

    private static StringContent BuildHttpContent(CarBookingRequest carBookingRequest)
        => new(JsonSerializer.Serialize(carBookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private void AssertCarBookingRequestsSentToBff(CarBookingRequest carBookingRequest)
            => _context.MockCarBookingService.Verify(m => m.BookAsync(MapToBooking(carBookingRequest),
                It.IsAny<string>()), Times.Once);

    private static CarBooking MapToBooking(CarBookingRequest carBookingRequest)
        => new(carBookingRequest.FirstName, carBookingRequest.LastName, carBookingRequest.StartDate,
            carBookingRequest.EndDate, carBookingRequest.Destination, carBookingRequest.Price);

    private static async Task<CarBookingResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<CarBookingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertBookingResponseEqualToCarBookingRequest(CarBookingRequest carBookingRequest,
        CarBookingResponse? bookingResponse)
    {
        Assert.Equal(bookingResponse?.FirstName, carBookingRequest?.FirstName);
        Assert.Equal(bookingResponse?.LastName, carBookingRequest?.LastName);
        Assert.Equal(bookingResponse?.StartDate, carBookingRequest?.StartDate);
        Assert.Equal(bookingResponse?.EndDate, carBookingRequest?.EndDate);
        Assert.Equal(bookingResponse?.Destination, carBookingRequest?.Destination);
        Assert.Equal(bookingResponse?.Price, carBookingRequest?.Price);
    }

    private static CarBookingRequest BuildCarBookingRequest(string firstName, string lastName, int daysTillStartDate,
        int daysTillEndDate, string destination, decimal price)
            => new(firstName, lastName, DateTime.UtcNow.AddDays(daysTillStartDate),
                DateTime.UtcNow.AddDays(daysTillEndDate), destination, price);

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues(_correlationIdHeader, out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }
}
