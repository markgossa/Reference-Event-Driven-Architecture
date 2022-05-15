using BookingGenerator.Domain.Models;

namespace BookingGenerator.Application.Repositories;

public interface IBookingService
{
    Task BookAsync(Booking booking, string? correlationId = null);
}
