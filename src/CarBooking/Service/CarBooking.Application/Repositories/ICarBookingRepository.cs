namespace CarBooking.Application.Repositories;

public interface ICarBookingRepository
{
    Task SendAsync(Domain.Models.CarBooking carBooking);
}
