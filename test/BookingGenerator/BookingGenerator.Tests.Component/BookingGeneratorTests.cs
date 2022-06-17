using BookingGenerator.Api.Models;
using BookingGenerator.Domain.Models;
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
    private const string _apiRoute = "bookings";
    private const string _apiRouteV1 = "v1/bookings";
    private const string _correlationIdHeader = "X-Correlation-Id";
    private readonly ApiTestsContext _context;

    public BookingGeneratorTests(ApiTestsContext context) => _context = context;

    [Theory]
    [InlineData(_apiRoute)]
    [InlineData(_apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalled_ThenSubmitsBookingsToBffAndReturnsAccepted(
        string apiRoute)
    {
        var bookingRequest = BuildBookingRequest();

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);
        var bookingResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertBookingRequestsSentToBff(bookingRequest);
        AssertBookingResponseEqualToBookingRequest(bookingRequest, bookingResponse);
        AssertBookingIdAddedToResponse(bookingResponse);
    }

    [Theory]
    [InlineData(_apiRoute)]
    [InlineData(_apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalledWithoutCorrelationIdHeader_ThenReturnsNewCorrelationIdHeader(
        string apiRoute)
    {
        var bookingRequest = BuildBookingRequest();

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);
        var bookingResponse = await DeserializeResponse(httpResponse);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        AssertReturnsNewCorrelationId(httpResponse);
        Assert.NotNull(bookingResponse);
    }

    [Theory]
    [InlineData(_apiRoute)]
    [InlineData(_apiRouteV1)]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalledAndThereIsAnError_ThenReturnsInternalServerError(
        string apiRoute)
    {
        var bookingRequest = BuildBookingRequest(_context.FailureFirstName);

        var httpResponse = await MakeBookingAsync(bookingRequest, apiRoute);

        Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GivenValidBookingRequest_WhenPostEndpointCalledWithDuplicateCorrelationId_ThenReturnsConflict()
    {
        var bookingRequest = BuildBookingRequest(_context.DuplicateFirstName);

        var httpResponse = await MakeBookingAsync(bookingRequest, _apiRoute);

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> MakeBookingAsync(BookingRequest bookingRequest, string apiRoute)
        => await _context.HttpClient.PostAsync(apiRoute, BuildHttpContent(bookingRequest));

    private static StringContent BuildHttpContent(BookingRequest bookingRequest)
        => new(JsonSerializer.Serialize(bookingRequest), Encoding.UTF8, MediaTypeNames.Application.Json);

    private void AssertBookingRequestsSentToBff(BookingRequest bookingRequest)
        => _context.MockBookingService.Verify(m => m.BookAsync(MapToBooking(bookingRequest),
            It.IsAny<string>()), Times.Once);

    private Booking MapToBooking(BookingRequest bookingRequest)
        => new(_context.CorrelationId, MapToBookingSummary(bookingRequest), MapToCarBooking(bookingRequest), 
            MapToHotelBooking(bookingRequest), MapToFlightBooking(bookingRequest));

    private static FlightBooking MapToFlightBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Flight.OutboundFlightTime,
            bookingRequest.Flight.OutboundFlightNumber, bookingRequest.Flight.InboundFlightTime,
            bookingRequest.Flight.InboundFlightNumber);

    private static HotelBooking MapToHotelBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Hotel.NumberOfBeds, bookingRequest.Hotel.BreakfastIncluded,
            bookingRequest.Hotel.LunchIncluded, bookingRequest.Hotel.DinnerIncluded);

    private static CarBooking MapToCarBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Car.PickUpLocation,
            MapToAnotherEnum<Domain.Enums.Size>(bookingRequest.Car.Size.ToString()),
            MapToAnotherEnum<Domain.Enums.Transmission>(bookingRequest.Car.Transmission.ToString()));

    private static BookingSummary MapToBookingSummary(BookingRequest bookingRequest)
        => new(bookingRequest.BookingSummary.FirstName,
            bookingRequest.BookingSummary.LastName, bookingRequest.BookingSummary.StartDate,
            bookingRequest.BookingSummary.EndDate, bookingRequest.BookingSummary.Destination,
            bookingRequest.BookingSummary.Price);

    private static T MapToAnotherEnum<T>(string value) where T : struct
        => Enum.Parse<T>(value);

    private static async Task<BookingResponse?> DeserializeResponse(HttpResponseMessage httpResponse)
    {
        var json = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<BookingResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static void AssertBookingResponseEqualToBookingRequest(BookingRequest bookingRequest,
        BookingResponse? bookingResponse)
    {
        Assert.Equal(bookingResponse?.BookingSummary?.FirstName, bookingRequest?.BookingSummary?.FirstName);
        Assert.Equal(bookingResponse?.BookingSummary?.LastName, bookingRequest?.BookingSummary?.LastName);
        Assert.Equal(bookingResponse?.BookingSummary?.StartDate, bookingRequest?.BookingSummary?.StartDate);
        Assert.Equal(bookingResponse?.BookingSummary?.EndDate, bookingRequest?.BookingSummary?.EndDate);
        Assert.Equal(bookingResponse?.BookingSummary?.Destination, bookingRequest?.BookingSummary?.Destination);
        Assert.Equal(bookingResponse?.BookingSummary?.Price, bookingRequest?.BookingSummary?.Price);

        Assert.Equal(bookingResponse?.Car?.PickUpLocation, bookingRequest?.Car?.PickUpLocation);
        Assert.Equal(bookingResponse?.Car?.Size, bookingRequest?.Car?.Size);
        Assert.Equal(bookingResponse?.Car?.Transmission, bookingRequest?.Car?.Transmission);

        Assert.Equal(bookingResponse?.Hotel?.NumberOfBeds, bookingRequest?.Hotel?.NumberOfBeds);
        Assert.Equal(bookingResponse?.Hotel?.BreakfastIncluded, bookingRequest?.Hotel?.BreakfastIncluded);
        Assert.Equal(bookingResponse?.Hotel?.LunchIncluded, bookingRequest?.Hotel?.LunchIncluded);
        Assert.Equal(bookingResponse?.Hotel?.DinnerIncluded, bookingRequest?.Hotel?.DinnerIncluded);

        Assert.Equal(bookingResponse?.Flight?.OutboundFlightTime, bookingRequest?.Flight?.OutboundFlightTime);
        Assert.Equal(bookingResponse?.Flight?.OutboundFlightNumber, bookingRequest?.Flight?.OutboundFlightNumber);
        Assert.Equal(bookingResponse?.Flight?.InboundFlightTime, bookingRequest?.Flight?.InboundFlightTime);
        Assert.Equal(bookingResponse?.Flight?.InboundFlightNumber, bookingRequest?.Flight?.InboundFlightNumber);
    }

    private static BookingRequest BuildBookingRequest(string? firstName = null)
    {
        var randomString = Guid.NewGuid().ToString();
        var randomNumber = new Random().Next(1, 1000);
        var randomDate = DateTime.UtcNow.AddDays(randomNumber);
        var randomBool = randomNumber % 2 == 0;
        var randomCarSize = GetRandomEnum<Api.Enums.Size>();
        var randomCarTransmission = GetRandomEnum<Api.Enums.Transmission>();

        var bookingSummary = new BookingSummaryRequest(firstName ?? randomString, randomString, randomDate, randomDate, 
            randomString, randomNumber);
        var car = new CarBookingRequest(randomString, randomCarSize, randomCarTransmission);
        var hotel = new HotelBookingRequest(randomNumber, randomBool, randomBool, randomBool);
        var flight = new FlightBookingRequest(randomDate, randomString, randomDate, randomString);

        return new BookingRequest(bookingSummary, car, hotel, flight);
    }

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    private void AssertReturnsNewCorrelationId(HttpResponseMessage response)
    {
        response.Headers.TryGetValues(_correlationIdHeader, out var correlationIdValues);
        Assert.Equal(_context.CorrelationId.ToString(), correlationIdValues?.First());
    }

    private void AssertBookingIdAddedToResponse(BookingResponse? bookingResponse)
        => Assert.Equal(_context.CorrelationId, bookingResponse?.BookingId);

}
