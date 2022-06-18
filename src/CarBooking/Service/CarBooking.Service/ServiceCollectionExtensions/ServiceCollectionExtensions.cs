using CarBooking.Application.Common.Behaviours;
using CarBooking.Application.Repositories;
using CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;
using CarBooking.Infrastructure.Services;
using CarBooking.Service.Consumers;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarBooking.Service.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(RequestValidationBehaviour<,>).Assembly);
        services.AddMediatR(typeof(MakeCarBookingCommand));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration) 
        => services.AddSingleton<ICarBookingService, CarBookingService>();

    public static IServiceCollection AddMassTransitBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumers(typeof(BookingCreatedConsumer));

            o.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration["MessageBus:ServiceBusConnectionString"]);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
