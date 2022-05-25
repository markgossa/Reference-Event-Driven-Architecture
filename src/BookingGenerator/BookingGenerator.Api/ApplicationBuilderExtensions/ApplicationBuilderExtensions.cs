using BookingGenerator.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace BookingGenerator.Api.ApplicationBuilderExtensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandlerMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CustomExceptionHandlerMiddleware>();
}
