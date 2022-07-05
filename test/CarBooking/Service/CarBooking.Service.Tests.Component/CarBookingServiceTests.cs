using CarBooking.Service.Consumers;
using Contracts.Messages;
using Contracts.Messages.Enums;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CarBooking.Service.Tests.Component;
public class CarBookingServiceTests : IClassFixture<ServiceTestsContext>
{
    private readonly ServiceTestsContext _context;

    public CarBookingServiceTests(ServiceTestsContext context) => _context = context;

    [Fact]
    public async void GivenNewInstance_WhenABookingCreatedEventIsReceived_ThenMakesACarBooking()
    {
        var bookingCreated = BuildBookingCreated();
        Domain.Models.CarBooking? actualCarBooking = null;
        _context.MockCarBookingRepository.Setup(m => m.SendAsync(It.Is<Domain.Models.CarBooking>(c
                => c.BookingId == bookingCreated.BookingId.ToString())))
            .Callback<Domain.Models.CarBooking>(c => actualCarBooking = c);

        var webApplicationFactory = _context.WebApplicationFactory;
        var testHarness = await StartTestHarness(webApplicationFactory.Services);
        SendMessage(bookingCreated, webApplicationFactory.Services);

        await AssertMessageConsumedAsync(webApplicationFactory.Services, bookingCreated, testHarness);
        AssertCarBookingSentToCarBookingRepositoryAsync(bookingCreated, actualCarBooking);
    }

    [Fact]
    public async void GivenNewInstance_WhenABookingCreatedEventIsReceived_ThenSendsACarBookedEvent()
    {
        var bookingCreated = BuildBookingCreated();

        var webApplicationFactory = _context.WebApplicationFactory;
        var testHarness = await StartTestHarness(webApplicationFactory.Services);
        SendMessage(bookingCreated, webApplicationFactory.Services);

        await AssertMessageConsumedAsync(webApplicationFactory.Services, bookingCreated, testHarness);
        await AssertCarBookedMessageSentAsync(bookingCreated, testHarness);
    }

    private static BookingCreated BuildBookingCreated()
    {
        var randomString = Guid.NewGuid().ToString();
        var randomNumber = new Random().Next(1, 1000);
        var randomDate = DateTime.UtcNow.AddDays(randomNumber);
        var randomBool = randomNumber % 2 == 0;
        var randomCarSize = GetRandomEnum<Size>();
        var randomCarTransmission = GetRandomEnum<Transmission>();

        var bookingSummary = new BookingSummaryEventData(randomString, randomString, randomDate, randomDate,
            randomString, randomNumber);
        var car = new CarBookingEventData(randomString, randomCarSize, randomCarTransmission);
        var hotel = new HotelBookingEventData(randomNumber, randomBool, randomBool, randomBool);
        var flight = new FlightBookingEventData(randomDate, randomString, randomDate, randomString);

        return new(randomString, bookingSummary, car, hotel, flight);
    }

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    private static void SendMessage(BookingCreated bookingCreated, IServiceProvider provider)
    {
        var requestClient = provider.GetRequiredService<IBus>().CreateRequestClient<BookingCreated>();
        requestClient.GetResponse<BookingCreated>(bookingCreated);
    }

    private static async Task<ITestHarness> StartTestHarness(IServiceProvider provider)
    {
        var testHarness = provider.GetRequiredService<ITestHarness>();
        await testHarness.Start();

        return testHarness;
    }

    private static bool IsMessageReceived(IReceivedMessage<BookingCreated> receivedMessages, BookingCreated expectedMessage)
    {
        var bookingCreated = receivedMessages.MessageObject as BookingCreated;

        return bookingCreated?.BookingId == expectedMessage.BookingId;
    }

    private static async Task AssertMessageConsumedAsync(IServiceProvider serviceProvider, BookingCreated bookingCreated,
        ITestHarness testHarness)
    {
        Assert.True(await IsMessageConsumedByService(testHarness, bookingCreated));
        Assert.True(await IsMessageConsumedByConsumer(serviceProvider, bookingCreated));
    }

    private static async Task<bool> IsMessageConsumedByConsumer(IServiceProvider serviceProvider, BookingCreated bookingCreated)
        => await serviceProvider.GetRequiredService<IConsumerTestHarness<BookingCreatedConsumer>>()
            .Consumed.Any<BookingCreated>(x => IsMessageReceived(x, bookingCreated));

    private static async Task<bool> IsMessageConsumedByService(ITestHarness testHarness, BookingCreated bookingCreated)
        => await testHarness.Consumed.Any<BookingCreated>(x => IsMessageReceived(x, bookingCreated));

    private void AssertCarBookingSentToCarBookingRepositoryAsync(BookingCreated bookingCreated, Domain.Models.CarBooking? carBooking)
    {
        Assert.Equal(bookingCreated.BookingId, carBooking?.BookingId);
        Assert.Equal(bookingCreated.CarBooking.PickUpLocation, carBooking?.PickUpLocation);
        Assert.Equal(bookingCreated.BookingSummary.FirstName, carBooking?.FirstName);
        Assert.Equal(bookingCreated.BookingSummary.LastName, carBooking?.LastName);
        Assert.Equal(bookingCreated.CarBooking.Size.ToString(), carBooking?.Size.ToString());
        Assert.Equal(bookingCreated.CarBooking.Transmission.ToString(), carBooking?.Transmission.ToString());

        _context.MockCarBookingRepository.Verify(m => m.SendAsync(It.Is<Domain.Models.CarBooking>(c
            => c.BookingId == bookingCreated.BookingId.ToString())), Times.Once);
    }

    private static async Task AssertCarBookedMessageSentAsync(BookingCreated bookingCreated, ITestHarness testHarness)
    {
        var publishedMessages = testHarness.Published.Select(m => m.MessageObject is CarBooked);
        Assert.True(await testHarness.Published.Any<CarBooked>());
        Assert.Single(publishedMessages);

        AssertPublishedMessageProperties(bookingCreated, (CarBooked)publishedMessages.First().MessageObject);
    }

    private static void AssertPublishedMessageProperties(BookingCreated bookingCreated, CarBooked actualCarBooked)
    {
        Assert.Equal(bookingCreated.BookingId, actualCarBooked.BookingId);
        Assert.Equal(bookingCreated.BookingSummary?.FirstName, actualCarBooked.BookingSummary?.FirstName);
        Assert.Equal(bookingCreated.BookingSummary?.LastName, actualCarBooked.BookingSummary?.LastName);
        Assert.Equal(bookingCreated.BookingSummary?.StartDate, actualCarBooked.BookingSummary?.StartDate);
        Assert.Equal(bookingCreated.BookingSummary?.EndDate, actualCarBooked.BookingSummary?.EndDate);
        Assert.Equal(bookingCreated.BookingSummary?.Destination, actualCarBooked.BookingSummary?.Destination);
        Assert.Equal(bookingCreated.BookingSummary?.Price, actualCarBooked.BookingSummary?.Price);
        Assert.Equal(bookingCreated.CarBooking?.PickUpLocation, actualCarBooked.CarBooking?.PickUpLocation);
        Assert.Equal(bookingCreated.CarBooking?.Size, actualCarBooked.CarBooking?.Size);
        Assert.Equal(bookingCreated.CarBooking?.Transmission, actualCarBooked.CarBooking?.Transmission);
        Assert.Equal(bookingCreated.HotelBooking?.NumberOfBeds, actualCarBooked.HotelBooking?.NumberOfBeds);
        Assert.Equal(bookingCreated.HotelBooking?.BreakfastIncluded, actualCarBooked.HotelBooking?.BreakfastIncluded);
        Assert.Equal(bookingCreated.HotelBooking?.LunchIncluded, actualCarBooked.HotelBooking?.LunchIncluded);
        Assert.Equal(bookingCreated.HotelBooking?.DinnerIncluded, actualCarBooked.HotelBooking?.DinnerIncluded);
        Assert.Equal(bookingCreated.FlightBooking?.OutboundFlightTime, actualCarBooked.FlightBooking?.OutboundFlightTime);
        Assert.Equal(bookingCreated.FlightBooking?.OutboundFlightNumber, actualCarBooked.FlightBooking?.OutboundFlightNumber);
        Assert.Equal(bookingCreated.FlightBooking?.InboundFlightTime, actualCarBooked.FlightBooking?.InboundFlightTime);
        Assert.Equal(bookingCreated.FlightBooking?.InboundFlightNumber, actualCarBooked.FlightBooking?.InboundFlightNumber);
    }
}
