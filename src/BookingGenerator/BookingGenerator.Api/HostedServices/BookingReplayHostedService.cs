using BookingGenerator.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingGenerator.Api.HostedServices;

internal class BookingReplayHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingReplayHostedService> _logger;

    public BookingReplayHostedService(IServiceProvider serviceProvider,
        ILogger<BookingReplayHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting BookingReplayHostedService");

        while (!cancellationToken.IsCancellationRequested)
        {
            await ReplayMessagesAsync();
            await Task.Delay(1000, cancellationToken);
        }

        _logger.LogInformation("Shutting down BookingReplayHostedService");
    }

    private async Task ReplayMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IBookingReplayService>()
            .ReplayBookingsAsync();
    }
}
