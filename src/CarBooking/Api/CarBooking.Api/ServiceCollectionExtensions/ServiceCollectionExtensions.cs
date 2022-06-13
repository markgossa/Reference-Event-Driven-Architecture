using CarBooking.Application.Common.Behaviours;
using CarBooking.Application.Repositories;
using CarBooking.Application.Services.Bookings.Commands.MakeBooking;
using CarBooking.Infrastructure;
using CarBooking.Infrastructure.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarBooking.Api.ServiceCollectionExtensions;

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
        services.AddScoped<ICarBookingRepository, CarBookingSqlRepository>();
        services.AddDbContextPool<CarBookingDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:CarBookings"]));

        return services;
    }
}
