using LSE.Stocks.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace LSE.Stocks.Api.ApplicationBuilderExtensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandlerMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CustomExceptionHandlerMiddleware>();
    
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CorrelationIdMiddleware>();
}
