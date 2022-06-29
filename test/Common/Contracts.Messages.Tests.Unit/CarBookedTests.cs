using Contracts.Messages.Enums;

namespace Contracts.Messages.Tests.Unit;

public class CarBookedTests
{
    [Fact]
    public void GivenNewInstance_WhenCreated_ThenCanSetTheCorrectProperties()
    {
        var bookingId = TestHelpers.GetRandomString();

        var firstName = TestHelpers.GetRandomString();
        var lastName = TestHelpers.GetRandomString();
        var destination = TestHelpers.GetRandomString();
        var price = TestHelpers.GetRandomNumber();
        var startDate = TestHelpers.GetRandomDate();
        var endDate = TestHelpers.GetRandomDate();

        var pickUpLocation = TestHelpers.GetRandomString();
        var size = TestHelpers.GetRandomEnum<Size>();
        var transmission = TestHelpers.GetRandomEnum<Transmission>();

        var numberOfBeds = TestHelpers.GetRandomNumber();
        var breakfastIncluded = TestHelpers.GetRandomBool();
        var lunchIncluded = TestHelpers.GetRandomBool();
        var dinnerIncluded = TestHelpers.GetRandomBool();

        var outboundFlightTime = TestHelpers.GetRandomDate();
        var outboundFlightNumber = TestHelpers.GetRandomString();
        var inboundFlightTime = TestHelpers.GetRandomDate();
        var inboundFlightNumber = TestHelpers.GetRandomString();

        var bookingSummary = new BookingSummaryEventData(firstName, lastName, startDate, endDate,
            destination, price);
        var car = new CarBookingEventData(pickUpLocation, size, transmission);
        var hotel = new HotelBookingEventData(numberOfBeds, breakfastIncluded, lunchIncluded, dinnerIncluded);
        var flight = new FlightBookingEventData(outboundFlightTime, outboundFlightNumber, inboundFlightTime, inboundFlightNumber);

        var sut = new CarBooked(bookingId, bookingSummary, car, hotel, flight);
        
        TestHelpers.AssertPropertiesCorrect(bookingId, firstName, lastName, destination, price, startDate, endDate, 
            pickUpLocation, size, transmission, numberOfBeds, breakfastIncluded, lunchIncluded, dinnerIncluded,
            outboundFlightTime, outboundFlightNumber, inboundFlightTime, inboundFlightNumber, sut);
    }
}