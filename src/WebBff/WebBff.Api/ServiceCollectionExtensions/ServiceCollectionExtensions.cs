using Common.Messaging.Folder;
using Common.Messaging.Folder.Repositories;
using Common.Messaging.Repository.Sql;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebBff.Api.HostedServices;
using WebBff.Application.Common.Behaviours;
using WebBff.Application.Infrastructure;
using WebBff.Application.Services.Bookings.Commands.MakeBooking;
using WebBff.Domain.Models;
using WebBff.Infrastructure;
using WebBff.Infrastructure.Interfaces;

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
        services.AddScoped<IMessageBus, AzureMessageBus>();
        services.AddScoped<IMessageBusOutbox, MessageBusOutbox>();
        services.AddScoped<IMessageOutbox<Booking>, MessageFolder<Booking>>();
        services.AddScoped<IMessageRepository<Booking>, SqlMessageRepository<Booking>>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();
        services.AddDbContextPool<MessageDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:Outbox"]));

        return services;
    }

    public static IServiceCollection AddBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();
            o.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration["MessageBus:ServiceBusConnectionString"]);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services)
        => services.AddHostedService<MessageProcessorHostedService>()
            .AddHostedService<PurgeMessagesHostedService<Booking>>();
}
