using FluentValidation;
using LSE.Stocks.Api.Services;
using LSE.Stocks.Application.Common.Behaviours;
using LSE.Stocks.Application.Repositories;
using LSE.Stocks.Application.Services.Shares.Commands.SaveTrade;
using LSE.Stocks.Application.Services.Shares.Queries.GetSharePrice;
using LSE.Stocks.Infrastructure;
using LSE.Stocks.Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LSE.Stocks.Api.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(GetSharePriceQueryValidator).Assembly);
        services.AddMediatR(typeof(SaveTradeCommand));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TradesDbContext>(o => o.UseSqlServer(configuration["ConnectionStrings:Trades"]));
        services.AddScoped<ITradeRepository, TradeSqlRepository>();
        services.AddScoped<ISharePriceRepository, SharePriceSqlRepository>();
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();

        return services;
    }
}
