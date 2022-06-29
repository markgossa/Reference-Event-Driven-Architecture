using CarBooking.Service.ServiceCollectionExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarBooking.Service;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
        => services.AddEndpointsApiExplorer()
            .AddMediatorServices()
            .AddApplicationInsightsTelemetry()
            .AddServices(Configuration)
            .AddBus(Configuration);

    public void Configure()
    {
    }
}
