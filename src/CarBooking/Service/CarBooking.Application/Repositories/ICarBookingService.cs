namespace CarBooking.Application.Repositories;

public interface ICarBookingService
{
    Task SendAsync(Domain.Models.CarBooking carBooking);
}
