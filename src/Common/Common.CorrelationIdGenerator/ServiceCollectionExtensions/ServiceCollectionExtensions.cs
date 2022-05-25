using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.CorrelationIdGenerator.ServiceCollectionExtensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationIdGenerator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();

        return services;
    }
}
