
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebBff.Infrastructure.Interfaces;

namespace WebBff.Api.HostedServices;

internal class MessageProcessorHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageProcessorHostedService> _logger;

    public MessageProcessorHostedService(IServiceProvider serviceProvider,
        ILogger<MessageProcessorHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {nameof(MessageProcessorHostedService)}");

        while (!cancellationToken.IsCancellationRequested)
        {
            await ReplayMessagesAsync();
            await Task.Delay(1000, cancellationToken);
        }

        _logger.LogInformation($"Shutting down {nameof(MessageProcessorHostedService)}");
    }

    private async Task ReplayMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageProcessor>()
            .PublishBookingCreatedMessagesAsync();
    }
}
