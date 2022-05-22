using Common.Messaging.CorrelationIdGenerator;
using Common.Messaging.Outbox;
using Common.Messaging.Outbox.Repositories;
using Common.Messaging.Outbox.Sql;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebBff.Api.HostedServices;
using WebBff.Application.Common.Behaviours;
using WebBff.Application.Services.Bookings.Commands.MakeBooking;
using WebBff.Domain.Models;

namespace WebBff.Api.ServiceCollectionExtensions;

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
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddScoped<IMessageOutbox<Booking>, MessageOutbox<Booking>>();
        services.AddScoped<IOutboxMessageRepository<Booking>, SqlOutboxMessageRepository<Booking>>();
        services.AddDbContextPool<OutboxMessageDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:Outbox"]));

        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services)
        => services.AddHostedService<BookingReplayHostedService>();
}
