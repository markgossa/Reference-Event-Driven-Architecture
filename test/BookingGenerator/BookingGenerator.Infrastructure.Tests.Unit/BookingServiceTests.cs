using AspNet.CorrelationIdGenerator;
using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using Moq;
using System.Net.Http;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceTests
{
    private const string _failureFirstName = "Unlucky";

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff()
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking();
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();

        await MakeNewBookingAsync(mockWebBffHttpClient.Object, SetUpMockCorrelationIdGenerator(correlationId),
            booking);

        mockWebBffHttpClient.Verify(m => m.PostAsync(booking, correlationId));
    }

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncWithCorrelationId_PostsRequestToWebBffWithSameCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking();
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();

        await MakeNewBookingWithCorrelationIdAsync(correlationId, mockWebBffHttpClient.Object, booking);

        mockWebBffHttpClient.Verify(m => m.PostAsync(booking, correlationId));
    }

    [Fact]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncAndThereIsAnError_ThrowsException()
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(_failureFirstName);
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();
        mockWebBffHttpClient.Setup(m => m.PostAsync(It.Is<Booking>(b => b.BookingSummary.FirstName == _failureFirstName), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(async ()
            => await MakeNewBookingAsync(mockWebBffHttpClient.Object, SetUpMockCorrelationIdGenerator(correlationId), booking));
    }

    private static async Task MakeNewBookingAsync(IWebBffHttpClient webBffHttpClient,
        ICorrelationIdGenerator mockCorrelationIdGenerator, Booking booking)
            => await new BookingService(webBffHttpClient, mockCorrelationIdGenerator)
                .BookAsync(booking);

    private static Booking BuildNewBooking(string? firstName = null)
    {
        var randomString = Guid.NewGuid().ToString();
        var randomNumber = new Random().Next(1, 1000);
        var randomDate = DateTime.UtcNow.AddDays(randomNumber);
        var randomBool = randomNumber % 2 == 0;
        var randomCarSize = GetRandomEnum<Domain.Enums.Size>();
        var randomCarTransmission = GetRandomEnum<Domain.Enums.Transmission>();

        var bookingSummary = new BookingSummary(firstName ?? randomString, randomString, randomDate, randomDate, randomString, randomNumber);
        var car = new CarBooking(randomString, randomCarSize, randomCarTransmission);
        var hotel = new HotelBooking(randomNumber, randomBool, randomBool, randomBool);
        var flight = new FlightBooking(randomDate, randomString, randomDate, randomString);

        return new Booking(randomString, bookingSummary, car, hotel, flight);
    }

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    private static ICorrelationIdGenerator SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(correlationId);

        return mockCorrelationIdGenerator.Object;
    }

    private static async Task MakeNewBookingWithCorrelationIdAsync(string correlationId,
        IWebBffHttpClient webBffHttpClient, Booking booking)
            => await new BookingService(webBffHttpClient, new CorrelationIdGenerator())
                .BookAsync(booking, correlationId);
}
