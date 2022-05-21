using BookingGenerator.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace BookingGenerator.Api.HostedServices;

internal class BookingReplayHostedService : IHostedService
{
    private readonly IBookingReplayService _bookingReplayService;

    public BookingReplayHostedService(IBookingReplayService bookingReplayService) 
        => _bookingReplayService = bookingReplayService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            await _bookingReplayService.ReplayBookingsAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();
}