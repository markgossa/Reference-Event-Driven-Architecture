using Microsoft.AspNetCore.Builder;

namespace Common.CorrelationIdGenerator.ApplicationBuilderExtensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseMiddleware<CorrelationIdMiddleware>();
}
