namespace BookingGenerator.Infrastructure;

public interface IBookingReplayService
{
    Task ReplayBookingsAsync();
}