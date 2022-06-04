using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebBff.Application.Common.Behaviours;
using WebBff.Application.Repositories;
using WebBff.Application.Services.Bookings.Commands.MakeBooking;
using WebBff.Infrastructure;

namespace WebBff.Api.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(RequestValidationBehaviour<,>).Assembly);
        services.AddMediatR(ConfigureMediatR, typeof(MakeBookingCommand));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));

        return services;
    }

    private static void ConfigureMediatR(MediatRServiceConfiguration configuration)
        => configuration.AsScoped();

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBookingRepository, BookingServiceBusRepository>();

        return services;
    }

    public static IServiceCollection AddBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();
            o.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration["ServiceBusConnectionString"]);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
