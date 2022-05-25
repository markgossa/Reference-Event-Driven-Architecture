using Microsoft.AspNetCore.Builder;
using WebBff.Api.Middleware;

namespace WebBff.Api.ApplicationBuilderExtensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandlerMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CustomExceptionHandlerMiddleware>();
}
