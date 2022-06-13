using CarBooking.Domain.Models;

namespace CarBooking.Application.Repositories;

public interface ICarBookingRepository
{
    Task SaveAsync(Domain.Models.CarBooking carBooking);
}
