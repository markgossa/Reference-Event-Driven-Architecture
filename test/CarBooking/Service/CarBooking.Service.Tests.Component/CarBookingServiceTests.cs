using CarBooking.Application.Repositories;
using CarBooking.Service.Consumers;
using Contracts.Messages;
using Contracts.Messages.Enums;
using MassTransit;
using Moq;
using Xunit;

namespace CarBooking.Service.Tests.Component;
public class CarBookingServiceTests
{
    [Fact]
    public async void GivenNewBookingCreatedEvent_WhenThisIsReceived_ThenMakesACarBookingAndSendsACarBookedEvent()
    {
        var mockCarBookingService = new Mock<ICarBookingService>();
        var mockConsumeContext = BuildMockConsumeContext();
        Domain.Models.CarBooking? actualCarBooking = null;
        mockCarBookingService.Setup(m => m.SendAsync(It.Is<Domain.Models.CarBooking>(c
                => c.BookingId == mockConsumeContext.Object.Message.BookingId.ToString())))
            .Callback<Domain.Models.CarBooking>(c => actualCarBooking = c);

        var sut = new BookingCreatedConsumer(mockCarBookingService.Object);
        await sut.Consume(mockConsumeContext.Object);

        var expectedBookingSummary = mockConsumeContext.Object.Message.BookingSummary;
        var expectedCarBooking = mockConsumeContext.Object.Message.CarBooking;
        Assert.Equal(mockConsumeContext.Object.Message.BookingId, actualCarBooking?.BookingId);
        Assert.Equal(expectedCarBooking.PickUpLocation, actualCarBooking?.PickUpLocation);
        Assert.Equal(expectedBookingSummary.FirstName, actualCarBooking?.FirstName);
        Assert.Equal(expectedBookingSummary.LastName, actualCarBooking?.LastName);
        Assert.Equal(expectedCarBooking.Size.ToString(), actualCarBooking?.Size.ToString());
        Assert.Equal(expectedCarBooking.Transmission.ToString(), actualCarBooking?.Transmission.ToString());
    }

    private static Mock<ConsumeContext<BookingCreated>> BuildMockConsumeContext()
    {
        var mockConsumeContext = new Mock<ConsumeContext<BookingCreated>>();
        var bookingCreated = BuildBookingCreated();
        mockConsumeContext.Setup(m => m.Message).Returns(bookingCreated);

        return mockConsumeContext;
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
}