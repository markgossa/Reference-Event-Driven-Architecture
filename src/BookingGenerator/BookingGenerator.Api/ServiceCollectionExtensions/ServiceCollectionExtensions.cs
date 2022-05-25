using BookingGenerator.Api.HostedServices;
using BookingGenerator.Application.Common.Behaviours;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;
using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure;
using BookingGenerator.Infrastructure.HttpClients;
using Common.CorrelationIdGenerator;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Repositories;
using Common.Messaging.Repository.Sql;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

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
        services.AddScoped<IBookingReplayService, BookingReplayService>(sp => BuildBookingReplayService(sp));
        services.AddScoped<IMessageOutbox<Booking>, MessageFolder<Booking>>();
        services.AddScoped<IMessageRepository<Booking>, SqlMessageRepository<Booking>>();
        services.AddDbContextPool<MessageDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:Outbox"]));
        AddWebBffHttpClient(services, configuration);

        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services)
        => services.AddHostedService<BookingReplayHostedService>();

    private static BookingServiceWithOutbox BuildBookingServiceWithOutbox(IServiceProvider sp) 
        => new(BuildBookingService(sp), sp.GetRequiredService<ICorrelationIdGenerator>(),
            sp.GetRequiredService<IMessageOutbox<Booking>>(), sp.GetRequiredService<ILogger<BookingServiceWithOutbox>>());

    private static IBookingService BuildBookingService(IServiceProvider sp) 
        => new BookingService(sp.GetRequiredService<IWebBffHttpClient>(), sp.GetRequiredService<ICorrelationIdGenerator>());

    private static BookingReplayService BuildBookingReplayService(IServiceProvider sp) 
        => new(BuildBookingService(sp),
                sp.GetRequiredService<IMessageOutbox<Booking>>(), sp.GetRequiredService<ILogger<BookingReplayService>>());

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
