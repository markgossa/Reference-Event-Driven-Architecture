using Common.Messaging.Folder.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.Messaging.Folder;

public class PurgeMessagesHostedService<T> : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PurgeMessagesHostedService<T>> _logger;

    public PurgeMessagesHostedService(IServiceProvider serviceProvider,
        ILogger<PurgeMessagesHostedService<T>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PurgeMessagesHostedService");

        while (!cancellationToken.IsCancellationRequested)
        {
            await RemoveOldMessagesAsync();
            await Task.Delay(10000, cancellationToken);
        }

        _logger.LogInformation("Shutting down PurgeMessagesHostedService");
    }

    private async Task RemoveOldMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageRepository<T>>()
            .RemoveAsync(minMessageAgeMinutes: 10);
    }
}
