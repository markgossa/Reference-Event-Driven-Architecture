using CarBooking.Application.Repositories;
using CarBooking.Service.Consumers;
using Contracts.Messages;
using Contracts.Messages.Enums;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CarBooking.Service.Tests.Component;
public class CarBookingServiceTests
{
    [Fact]
    public async void GivenNewBookingCreatedEvent_WhenThisIsReceived_ThenMakesACarBookingAndSendsACarBookedEvent()
    {
        var mockCarBookingService = new Mock<ICarBookingService>();
        var serviceProvider = BuildServiceProvider(mockCarBookingService);
        var bookingCreated = BuildBookingCreated();
        Domain.Models.CarBooking? actualCarBooking = null;
        mockCarBookingService.Setup(m => m.SendAsync(It.Is<Domain.Models.CarBooking>(c
                => c.BookingId == bookingCreated.BookingId.ToString())))
            .Callback<Domain.Models.CarBooking>(c => actualCarBooking = c);

        var testHarness = await StartTestHarness(serviceProvider);
        SendMessage(bookingCreated, serviceProvider);

        await AssertMessageConsumedAsync(serviceProvider, bookingCreated, testHarness);
        AssertCarBookingSentAsync(bookingCreated, actualCarBooking);
    }

    //public async void GivenNewBookingCreatedEvent_WhenThisIsReceived_ThenSendsACarBookedEvent(){}

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

    private static ServiceProvider BuildServiceProvider(Mock<ICarBookingService> mockCarBookingService)
        => new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<BookingCreatedConsumer>())
            .AddSingleton(mockCarBookingService.Object)
            .BuildServiceProvider(true);

    private static void SendMessage(BookingCreated bookingCreated, ServiceProvider provider)
    {
        var requestClient = provider.GetRequiredService<IBus>().CreateRequestClient<BookingCreated>();
        requestClient.GetResponse<BookingCreated>(bookingCreated);
    }

    private static async Task<ITestHarness> StartTestHarness(ServiceProvider provider)
    {
        var testHarness = provider.GetRequiredService<ITestHarness>();
        await testHarness.Start();

        return testHarness;
    }

    private static async Task<bool> IsMessageConsumedByConsumer(ServiceProvider serviceProvider, BookingCreated bookingCreated)
        => await serviceProvider.GetRequiredService<IConsumerTestHarness<BookingCreatedConsumer>>()
                .Consumed.Any<BookingCreated>(x => IsMessageReceived(x, bookingCreated));

    private static async Task<bool> IsMessageConsumedByService(ITestHarness testHarness, BookingCreated bookingCreated)
        => await testHarness.Consumed.Any<BookingCreated>(x => IsMessageReceived(x, bookingCreated));
    
    private static bool IsMessageReceived(IReceivedMessage<BookingCreated> receivedMessages, BookingCreated expectedMessage)
    {
        var bookingCreated = receivedMessages.MessageObject as BookingCreated;

        return bookingCreated?.BookingId == expectedMessage.BookingId;
    }

    private static async Task AssertMessageConsumedAsync(ServiceProvider serviceProvider, BookingCreated bookingCreated, ITestHarness testHarness)
    {
        Assert.True(await IsMessageConsumedByService(testHarness, bookingCreated));
        Assert.True(await IsMessageConsumedByConsumer(serviceProvider, bookingCreated));
    }

    private static void AssertCarBookingSentAsync(BookingCreated bookingCreated, Domain.Models.CarBooking? carBooking)
    {
        Assert.Equal(bookingCreated.BookingId, carBooking?.BookingId);
        Assert.Equal(bookingCreated.CarBooking.PickUpLocation, carBooking?.PickUpLocation);
        Assert.Equal(bookingCreated.BookingSummary.FirstName, carBooking?.FirstName);
        Assert.Equal(bookingCreated.BookingSummary.LastName, carBooking?.LastName);
        Assert.Equal(bookingCreated.CarBooking.Size.ToString(), carBooking?.Size.ToString());
        Assert.Equal(bookingCreated.CarBooking.Transmission.ToString(), carBooking?.Transmission.ToString());
    }
}
