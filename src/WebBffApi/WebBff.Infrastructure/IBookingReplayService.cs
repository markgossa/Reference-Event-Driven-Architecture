namespace WebBff.Infrastructure;

public interface IBookingReplayService
{
    Task ReplayBookingsAsync();
}