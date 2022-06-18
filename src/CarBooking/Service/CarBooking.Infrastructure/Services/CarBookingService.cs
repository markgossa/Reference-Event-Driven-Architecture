using CarBooking.Application.Repositories;

namespace CarBooking.Infrastructure.Services;
public class CarBookingService : ICarBookingService
{
    public Task SendAsync(Domain.Models.CarBooking carBooking) => throw new NotImplementedException();
}
