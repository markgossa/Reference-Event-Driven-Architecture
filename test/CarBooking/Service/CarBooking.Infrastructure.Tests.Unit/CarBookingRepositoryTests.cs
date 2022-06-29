using CarBooking.Domain.Enums;
using CarBooking.Infrastructure.Clients;
using CarBooking.Infrastructure.Models;
using CarBooking.Infrastructure.Services;
using Moq;

namespace CarBooking.Infrastructure.Tests.Unit;
public class CarBookingRepositoryTests
{
    private readonly Random _random = new();
    private readonly Mock<ICarBookingHttpClient> _mockCarBookingHttpClient = new();

    [Fact]
    public async Task GivenNewInstance_WhenISaveCarBookingAsync_ThenSendsBookingToCarBookingApi()
    {
        var carBooking = BuildNewCarBooking();

        var sut = new CarBookingRepository(_mockCarBookingHttpClient.Object);
        await sut.SendAsync(carBooking);

        _mockCarBookingHttpClient.Verify(m => m.PostAsync(It.Is<CarBookingRequest>(c
            => ItIsExpectedCarBookingRequest(c, carBooking))), Times.Once);
    }
    
    private Domain.Models.CarBooking BuildNewCarBooking()
        => new(GetRandomString(), GetRandomString(), GetRandomString(),
            GetRandomDate(), GetRandomDate(), GetRandomString(), GetRandomNumber(),
            GetRandomEnum<Size>(), GetRandomEnum<Transmission>());

    private DateTime GetRandomDate() => DateTime.Now.AddDays(GetRandomNumber());

    private int GetRandomNumber() => _random.Next(1000, 9000);

    private static string GetRandomString() => Guid.NewGuid().ToString();

    private static T GetRandomEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var length = values.Length;

        return values[new Random().Next(0, length)];
    }

    private static bool ItIsExpectedCarBookingRequest(CarBookingRequest carBookingRequest,
        Domain.Models.CarBooking carBooking)
            => carBookingRequest.BookingId == carBooking.BookingId
                && carBookingRequest.FirstName == carBooking.FirstName
                && carBookingRequest.LastName == carBooking.LastName
                && carBookingRequest.StartDate == carBooking.StartDate
                && carBookingRequest.EndDate == carBooking.EndDate
                && carBookingRequest.PickUpLocation == carBooking.PickUpLocation
                && carBookingRequest.Price == carBooking.Price
                && carBookingRequest.Size.ToString() == carBooking.Size.ToString()
                && carBookingRequest.Transmission.ToString() == carBooking.Transmission.ToString();
}
