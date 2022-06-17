using WebBff.Domain.Models;

namespace WebBff.Infrastructure.Tests.Unit;

public class MessageBusOutboxTestsBase
{
    protected static Booking BuildNewBooking(string? firstName = null)
    {
        var randomString = Guid.NewGuid().ToString();
        var randomNumber = new Random().Next(1, 1000);
        var randomDate = DateTime.UtcNow.AddDays(randomNumber);
        var randomBool = randomNumber % 2 == 0;
        var randomCarSize = GetRandomEnum<Domain.Enums.Size>();
        var randomCarTransmission = GetRandomEnum<Domain.Enums.Transmission>();

        var bookingSummary = new BookingSummary(firstName ?? randomString, randomString, randomDate, randomDate,
            randomString, randomNumber);
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
}