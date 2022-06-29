using Contracts.Messages.Enums;

namespace Contracts.Messages.Tests.Unit;

internal static class TestHelpers
{
    public static bool GetRandomBool() => GetRandomNumber() % 2 == 0;

    public static DateTime GetRandomDate() => DateTime.UtcNow.AddDays(GetRandomNumber());

    public static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    public static int GetRandomNumber() => new Random().Next(1, 1000);

    public static string GetRandomString() => Guid.NewGuid().ToString();

    public static void AssertPropertiesCorrect<T>(string bookingId, string firstName, string lastName, string destination,
        int price, DateTime startDate, DateTime endDate, string pickUpLocation, Size size, Transmission transmission, int numberOfBeds,
        bool breakfastIncluded, bool lunchIncluded, bool dinnerIncluded, DateTime outboundFlightTime, string outboundFlightNumber,
        DateTime inboundFlightTime, string inboundFlightNumber, T? sut)
        where T : BookingEventBase
    {
        Assert.Equal(bookingId, sut?.BookingId);
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
}