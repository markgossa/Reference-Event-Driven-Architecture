using Contracts.Messages.Enums;

namespace Contracts.Messages.Tests.Unit;

public class BookingCreatedTests
{
    [Fact]
    public void GivenNewInstance_WhenCreated_ThenCanSetTheCorrectProperties()
    {
        var bookingId = GetRandomString();

        var firstName = GetRandomString();
        var lastName = GetRandomString();
        var destination = GetRandomString();
        var price = GetRandomNumber();
        var startDate = GetRandomDate();
        var endDate = GetRandomDate();

        var pickUpLocation = GetRandomString();
        var size = GetRandomEnum<Size>();
        var transmission = GetRandomEnum<Transmission>();

        var numberOfBeds = GetRandomNumber();
        var breakfastIncluded = GetRandomBool();
        var lunchIncluded = GetRandomBool();
        var dinnerIncluded = GetRandomBool();

        var outboundFlightTime = GetRandomDate();
        var outboundFlightNumber = GetRandomString();
        var inboundFlightTime = GetRandomDate();
        var inboundFlightNumber = GetRandomString();

        var bookingSummary = new BookingSummaryEventData(firstName, lastName, startDate, endDate,
            destination, price);
        var car = new CarBookingEventData(pickUpLocation, size, transmission);
        var hotel = new HotelBookingEventData(numberOfBeds, breakfastIncluded, lunchIncluded, dinnerIncluded);
        var flight = new FlightBookingEventData(outboundFlightTime, outboundFlightNumber, inboundFlightTime, inboundFlightNumber);

        var sut = new BookingCreated(bookingId, bookingSummary, car, hotel, flight);

        Assert.Equal(firstName, sut?.BookingSummary?.FirstName);
        Assert.Equal(lastName, sut?.BookingSummary?.LastName);
        Assert.Equal(startDate, sut?.BookingSummary?.StartDate);
        Assert.Equal(endDate, sut?.BookingSummary?.EndDate);
        Assert.Equal(destination, sut?.BookingSummary?.Destination);
        Assert.Equal(price, sut?.BookingSummary?.Price);
        Assert.Equal(pickUpLocation, sut?.CarBooking?.PickUpLocation);
        Assert.Equal(size, sut?.CarBooking?.Size);
        Assert.Equal(transmission, sut?.CarBooking?.Transmission);
        Assert.Equal(numberOfBeds, sut?.HotelBooking?.NumberOfBeds);
        Assert.Equal(breakfastIncluded, sut?.HotelBooking?.BreakfastIncluded);
        Assert.Equal(lunchIncluded, sut?.HotelBooking?.LunchIncluded);
        Assert.Equal(dinnerIncluded, sut?.HotelBooking?.DinnerIncluded);
        Assert.Equal(outboundFlightTime, sut?.FlightBooking?.OutboundFlightTime);
        Assert.Equal(outboundFlightNumber, sut?.FlightBooking?.OutboundFlightNumber);
        Assert.Equal(inboundFlightTime, sut?.FlightBooking?.InboundFlightTime);
        Assert.Equal(inboundFlightNumber, sut?.FlightBooking?.InboundFlightNumber);
    }

    private static DateTime GetRandomDate() => DateTime.UtcNow.AddDays(GetRandomNumber());
    private static bool GetRandomBool() => GetRandomNumber() % 2 == 0;
    private static int GetRandomNumber() => new Random().Next(1, 1000);
    private static string GetRandomString() => Guid.NewGuid().ToString();

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }
}