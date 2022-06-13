using CarBooking.Api.Models;
using CarBooking.Domain.Enums;
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
    private const string _apiRoute = "carbookings";
    private const string _apiRouteV1 = "v1/carbookings";
    private const string _correlationIdHeader = "X-Correlation-Id";
    private readonly ApiTestsContext _context;

    public CarBookingApiTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Malta", 100.43, Size.Medium, Transmission.Manual, _apiRoute)]
    [InlineData("Mary", "Bloggs", 0, 5, "Malta", 100.43, Size.Large, Transmission.Manual, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Corfu", 100.43, Size.Medium, Transmission.Manual, _apiRouteV1)]
    [InlineData("Emilia", "Smith", 10, 15, "Corfu", 100.43, Size.Small, Transmission.Manual, _apiRouteV1)]
    public async Task GivenValidCarBookingRequest_WhenPostEndpointCalled_ThenSubmitsBookingsToBffAndReturnsAccepted(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, Size size, Transmission transmission, string apiRoute)
    {
        var carBookingRequest = BuildCarBookingRequest(Guid.NewGuid().ToString(), firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price, size, transmission);

        var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        AssertCarBookingSavedToRepository(carBookingRequest);
        AssertBookingResponseEqualToCarBookingRequest(carBookingRequest, await DeserializeResponse(httpResponse));

    }

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Greece", 100.43, Size.Small, Transmission.Manual, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Nice", 100.43, Size.Large, Transmission.Automatic, _apiRouteV1)]
    public async Task GivenValidCarBookingRequest_WhenPostEndpointCalled_ThenReturnsCorrelationIdHeaderSetToBookingRequestId(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, Size size, Transmission transmission, string apiRoute)
    {
        var id = Guid.NewGuid().ToString();
        var carBookingRequest = BuildCarBookingRequest(id, firstName, lastName, daysTillStartDate,
            daysTillEndDate, destination, price, size, transmission);

        var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        AssertReturnsNewCorrelationId(httpResponse, id);
        AssertCarBookingSavedToRepository(carBookingRequest);
        AssertBookingResponseEqualToCarBookingRequest(carBookingRequest, await DeserializeResponse(httpResponse));
    }

    [Theory]
    [InlineData(ApiTestsContext.ErrorBooking, "Bloggs", 0, 5, "Greece", 100.43, Size.Small, Transmission.Manual, _apiRoute)]
    [InlineData(ApiTestsContext.ErrorBooking, "Smith", 10, 15, "Nice", 100.43, Size.Large, Transmission.Automatic, _apiRouteV1)]
    public async Task GivenValidCarBookingRequest_WhenPostEndpointCalledAndThereIsAnError_ThenReturnsInternalServerError(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, Size size, Transmission transmission, string apiRoute)
    {
        var carBookingRequest = BuildCarBookingRequest(Guid.NewGuid().ToString(), firstName, lastName, daysTillStartDate,
            daysTillEndDate, destination, price, size, transmission);

        var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GivenValidCarBookingRequest_WhenPostEndpointCalledWithDuplicateRequest_ThenReturnsConflict()
    {
        var carBookingRequest = BuildCarBookingRequest(Guid.NewGuid().ToString(), ApiTestsContext.DuplicateBooking, "Bloggs", 5, 29,
            "Belize", 1700, Size.Large, Transmission.Automatic);

        var httpResponse = await MakeBookingAsync(carBookingRequest, _apiRoute);

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }

    [Theory]
    [InlineData("", "Jane", "Smith", 10, 15, "Corfu", 100.43, Size.Small, Transmission.Manual, _apiRouteV1)]
    [InlineData(null, "Jane", "Smith", 10, 15, "Corfu", 100.43, Size.Small, Transmission.Manual, _apiRouteV1)]
    [InlineData(" ", "Jane", "Smith", 10, 15, "Corfu", 100.43, Size.Small, Transmission.Manual, _apiRouteV1)]
    public async Task GivenCarBookingRequestAndIdIsEmpty_WhenPostEndpointCalled_ThenDoesNotSaveBookingsAndReturnsBadRequest(
        string id, string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, Size size, Transmission transmission, string apiRoute)
    {
        var carBookingRequest = BuildCarBookingRequest(id, firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price, size, transmission);

        var httpResponse = await MakeBookingAsync(carBookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        AssertCarBookingNotSavedToRepository(carBookingRequest);
    }

    private async Task<HttpResponseMessage> MakeBookingAsync(CarBookingRequest carBookingRequest, string apiRoute)
            => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(carBookingRequest));

    private static StringContent BuildHttpContent(CarBookingRequest carBookingRequest)
        => new(JsonSerializer.Serialize(carBookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private void AssertCarBookingSavedToRepository(CarBookingRequest carBookingRequest)
        => _context.MockCarBookingRepository.Verify(m => m.SaveAsync(MapToBooking(carBookingRequest)), Times.Once);

    private static Domain.Models.CarBooking MapToBooking(CarBookingRequest carBookingRequest)
        => new(carBookingRequest.Id, carBookingRequest.FirstName, carBookingRequest.LastName, carBookingRequest.StartDate,
                carBookingRequest.EndDate, carBookingRequest.PickUpLocation, carBookingRequest.Price,
                carBookingRequest.Size, carBookingRequest.Transmission);

    private static async Task<CarBookingResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<CarBookingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertBookingResponseEqualToCarBookingRequest(CarBookingRequest carBookingRequest,
        CarBookingResponse? carBookingResponse)
    {
        Assert.Equal(carBookingResponse?.Id, carBookingRequest?.Id);
        Assert.Equal(carBookingResponse?.FirstName, carBookingRequest?.FirstName);
        Assert.Equal(carBookingResponse?.LastName, carBookingRequest?.LastName);
        Assert.Equal(carBookingResponse?.StartDate, carBookingRequest?.StartDate);
        Assert.Equal(carBookingResponse?.EndDate, carBookingRequest?.EndDate);
        Assert.Equal(carBookingResponse?.PickUpLocation, carBookingRequest?.PickUpLocation);
        Assert.Equal(carBookingResponse?.Price, carBookingRequest?.Price);
        Assert.Equal(carBookingResponse?.Size, carBookingRequest?.Size);
        Assert.Equal(carBookingResponse?.Transmission, carBookingRequest?.Transmission);
    }

    private static CarBookingRequest BuildCarBookingRequest(string id, string firstName, string lastName, int daysTillStartDate,
        int daysTillEndDate, string pickUpLocation, decimal price, Size size, Transmission transmission)
            => new(id, firstName, lastName, DateTime.UtcNow.AddDays(daysTillStartDate),
                DateTime.UtcNow.AddDays(daysTillEndDate), pickUpLocation, price, size, transmission);

    private static void AssertReturnsNewCorrelationId(HttpResponseMessage response, string expectedCorrelationId)
    {
        response.Headers.TryGetValues(_correlationIdHeader, out var correlationIdValues);
        Assert.Equal(expectedCorrelationId, correlationIdValues?.First());
    }

    private void AssertCarBookingNotSavedToRepository(CarBookingRequest carBookingRequest)
        => _context.MockCarBookingRepository.Verify(m => m.SaveAsync(MapToBooking(carBookingRequest)), Times.Never);
}
