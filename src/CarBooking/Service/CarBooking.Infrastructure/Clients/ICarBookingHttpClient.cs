using CarBooking.Infrastructure.Models;

namespace CarBooking.Infrastructure.Clients;
public interface ICarBookingHttpClient
{
    Task PostAsync(CarBookingRequest carBookingRequest);
}
