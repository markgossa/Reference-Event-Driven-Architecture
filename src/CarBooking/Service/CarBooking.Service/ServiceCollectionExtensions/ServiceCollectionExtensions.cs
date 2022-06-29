using CarBooking.Application.Common.Behaviours;
using CarBooking.Application.Repositories;
using CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;
using CarBooking.Infrastructure.Clients;
using CarBooking.Infrastructure.Models;
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
    {
        services.AddSingleton<ICarBookingRepository, CarBookingRepository>()
            .AddHttpClient<ICarBookingHttpClient, CarBookingHttpClient>();

        services.AddOptions<CarBookingHttpClientSettings>()
            .Bind(configuration.GetSection(nameof(CarBookingHttpClientSettings)));

        return services;
    }

    public static IServiceCollection AddBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumers(typeof(BookingCreatedConsumer).Assembly);

            o.UsingAzureServiceBus((context, cfg) =>

            {
                cfg.Host(new Uri(configuration["MessageBus:ServiceBusUri"]));
                cfg.ConfigureEndpoints(context);
                cfg.UseMessageRetry(r => AddRetryConfiguration(r));
            });
        });

        return services;
    }

    private static IRetryConfigurator AddRetryConfiguration(IRetryConfigurator retryConfigurator)
    {
        retryConfigurator.Exponential(int.MaxValue, TimeSpan.FromMilliseconds(200), TimeSpan.FromMinutes(120), 
                TimeSpan.FromMilliseconds(200))
            .Ignore<ValidationException>();

        return retryConfigurator;
    }
}
