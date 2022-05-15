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
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        AddWebBffHttpClient(services, configuration);

        return services;
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
