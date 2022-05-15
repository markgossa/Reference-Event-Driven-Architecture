using BookingGenerator.Api.Models;
using BookingGenerator.Domain.Models;
using BookingGenerator.Tests.Component.DateTimeExtensions;
using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BookingGenerator.Tests.Component;

public class BookingGeneratorTests : IClassFixture<ApiTestsContext>
{
    private const string _apiRoute = "bulkbookings";
    private const string _apiRouteV1 = "v1/bulkbookings";
    private readonly ApiTestsContext _context;

    public BookingGeneratorTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Malta", 100.43, 10, _apiRoute)]
    [InlineData("Mary", "Bloggs", 0, 5, "Malta", 100.43, 5, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Corfu", 100.43, 10, _apiRouteV1)]
    [InlineData("Emilia", "Smith", 10, 15, "Corfu", 100.43, 5, _apiRouteV1)]
    public async Task GivenValidBulkBookingRequest_WhenPostEndpointCalled_ThenSubmitsBookingsToBffAndReturnsAccepted(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, int count, string apiRoute)
    {
        var bulkBookingRequest = BuildBulkBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate, 
            destination, price, count);

        var httpResponse = await MakeBookingAsync(bulkBookingRequest, apiRoute);
        var bulkBookingResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertBookingRequestsSentToBff(bulkBookingRequest, count);
        AssertBulkBookingResponseEqualToBulkBookingRequest(bulkBookingRequest, bulkBookingResponse);
    }

    [Theory]
    [InlineData("Joe", "Bloggs", 0, 5, "Greece", 100.43, 12, _apiRoute)]
    [InlineData("John", "Smith", 10, 15, "Nice", 100.43, 10, _apiRouteV1)]
    public async Task GivenValidTradeRequest_WhenPostEndpointCalledWithoutCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, int count, string apiRoute)
    {
        var bulkBookingRequest = BuildBulkBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price, count);

        var httpResponse = await MakeBookingAsync(bulkBookingRequest, apiRoute);
        await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertReturnsNewCorrelationId(httpResponse);
    }
    
    [Theory]
    [InlineData("Unlucky", "Bloggs", 0, 5, "Greece", 100.43, 12, _apiRoute)]
    [InlineData("Unlucky", "Smith", 10, 15, "Nice", 100.43, 10, _apiRouteV1)]
    public async Task GivenValidTradeRequest_WhenPostEndpointCalledAndThereIsAnError_ThenReturnsInternalServerError(
        string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string destination,
        decimal price, int count, string apiRoute)
    {
        var bulkBookingRequest = BuildBulkBookingRequest(firstName, lastName, daysTillStartDate, daysTillEndDate,
            destination, price, count);

        var httpResponse = await MakeBookingAsync(bulkBookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> MakeBookingAsync(BulkBookingRequest bulkBookingRequest, string apiRoute)
            => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(bulkBookingRequest));

    private static StringContent BuildHttpContent(BulkBookingRequest bulkBookingRequest)
        => new(JsonSerializer.Serialize(bulkBookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private void AssertBookingRequestsSentToBff(BulkBookingRequest bulkBookingRequest, int count)
            => _context.MockBookingService.Verify(m => m.BookAsync(MapToBooking(bulkBookingRequest.BookingRequests.First()), 
                It.IsAny<string>()), Times.Exactly(count));

    private static Booking MapToBooking(BookingRequest bookingRequest)
        => new(bookingRequest.FirstName, bookingRequest.LastName, bookingRequest.StartDate.ToDateOnly(),
            bookingRequest.EndDate.ToDateOnly(), bookingRequest.Destination, bookingRequest.Price);

    private static async Task<BulkBookingResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<BulkBookingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertBulkBookingResponseEqualToBulkBookingRequest(BulkBookingRequest bulkBookingRequest,
        BulkBookingResponse? bulkBookingResponse)
    {
        Assert.Equal(bulkBookingResponse?.BookingResponses.Count, bulkBookingRequest.BookingRequests.Count);

        for (var i = 0; i < bulkBookingRequest.BookingRequests.Count; i++)
        {
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].FirstName, bulkBookingRequest.BookingRequests[i]?.FirstName);
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].LastName, bulkBookingRequest.BookingRequests[i]?.LastName);
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].StartDate, bulkBookingRequest.BookingRequests[i]?.StartDate);
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].EndDate, bulkBookingRequest.BookingRequests[i]?.EndDate);
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].Destination, bulkBookingRequest.BookingRequests[i]?.Destination);
            Assert.Equal(bulkBookingResponse?.BookingResponses[i].Price, bulkBookingRequest.BookingRequests[i]?.Price);
        }
    }

    private static BulkBookingRequest BuildBulkBookingRequest(string firstName, string lastName, int daysTillStartDate,
        int daysTillEndDate, string destination, decimal price, int count)
    {
        var bookingRequest = new BookingRequest(firstName, lastName, DateTime.UtcNow.AddDays(daysTillStartDate),
                    DateTime.UtcNow.AddDays(daysTillEndDate), destination, price);

        var bookingRequests = new List<BookingRequest>();
        for (var i = 0; i < count; i++)
        {
            bookingRequests.Add(bookingRequest);
        }

        return new BulkBookingRequest(bookingRequests);
    }

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("X-Correlation-Id", out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }
}
