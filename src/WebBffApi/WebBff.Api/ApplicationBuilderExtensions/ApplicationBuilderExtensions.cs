using WebBff.Api.Middleware;
using Common.Messaging.CorrelationIdGenerator;
using Microsoft.AspNetCore.Builder;

namespace WebBff.Api.ApplicationBuilderExtensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandlerMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CustomExceptionHandlerMiddleware>();

    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CorrelationIdMiddleware>();
}
