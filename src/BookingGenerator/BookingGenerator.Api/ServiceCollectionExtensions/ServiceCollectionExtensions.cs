using Common.Messaging.CorrelationIdGenerator;
using BookingGenerator.Application.Common.Behaviours;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;
using BookingGenerator.Infrastructure;
using BookingGenerator.Infrastructure.HttpClients;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Common.Messaging.Outbox;
using BookingGenerator.Domain.Models;
using Common.Messaging.Outbox.Repositories;
using Common.Messaging.Outbox.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookingGenerator.Api.HostedServices;

namespace BookingGenerator.Api.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(RequestValidationBehaviour<,>).Assembly);
        services.AddMediatR(typeof(MakeBookingCommand));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBookingService, BookingServiceWithOutbox>(sp => BuildBookingServiceWithOutbox(sp));
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddScoped<IBookingReplayService, BookingReplayService>();
        services.AddScoped<IMessageOutbox<Booking>, MessageOutbox<Booking>>();
        services.AddScoped<IOutboxMessageRepository<Booking>, SqlOutboxMessageRepository<Booking>>();
        services.AddDbContextPool<OutboxMessageDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:Outbox"]));
        AddWebBffHttpClient(services, configuration);

        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services)
        => services.AddHostedService<BookingReplayHostedService>();

    private static BookingServiceWithOutbox BuildBookingServiceWithOutbox(IServiceProvider sp)
    {
        var bookingService = new BookingService(sp.GetRequiredService<IWebBffHttpClient>(),
            sp.GetRequiredService<ICorrelationIdGenerator>());

        return new(bookingService, sp.GetRequiredService<ICorrelationIdGenerator>(), 
            sp.GetRequiredService<IMessageOutbox<Booking>>(), sp.GetRequiredService<ILogger<BookingServiceWithOutbox>>());
    }

    private static void AddWebBffHttpClient(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(WebBffHttpClientSettings)).Get<WebBffHttpClientSettings>();
        services.AddHttpClient<IWebBffHttpClient, WebBffHttpClient>()
            .AddPolicyHandler(BuildRetryPolicy(settings));
        services.AddOptions<WebBffHttpClientSettings>().Bind(configuration.GetSection(nameof(WebBffHttpClientSettings)));
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy(WebBffHttpClientSettings settings)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(settings.MaxAttempts - 1,
                i => TimeSpan.FromMilliseconds(settings.InitialRetryIntervalMilliseconds * i));
}
