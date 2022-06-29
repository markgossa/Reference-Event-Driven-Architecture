using CarBooking.Application.Repositories;
using CarBooking.Infrastructure.Clients;
using CarBooking.Infrastructure.Enums;
using CarBooking.Infrastructure.Models;

namespace CarBooking.Infrastructure.Services;
public class CarBookingRepository : ICarBookingRepository
{
    private readonly ICarBookingHttpClient _carBookingApiClient;

    public CarBookingRepository(ICarBookingHttpClient carBookingApiClient)
        => _carBookingApiClient = carBookingApiClient;

    public async Task SendAsync(Domain.Models.CarBooking carBooking)
    {
        var carBookingRequest = new CarBookingRequest(carBooking.BookingId, carBooking.FirstName,
            carBooking.LastName, carBooking.StartDate, carBooking.EndDate, carBooking.PickUpLocation, carBooking.Price,
            MapToEnum<Size>(carBooking.Size.ToString()), MapToEnum<Transmission>(carBooking.Transmission.ToString()));

        await _carBookingApiClient.PostAsync(carBookingRequest);
    }

    private static T MapToEnum<T>(string value) where T : struct
        => Enum.Parse<T>(value);
}
