using LSE.Stocks.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LSE.Stocks.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string _correlationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ICorrelationIdService correlationIdService)
    {
        var correlationId = GetCorrelationId(context, correlationIdService);
        AddCorrelationIdHeaderToResponse(context, correlationId);

        await _next(context);
    }

    private static StringValues GetCorrelationId(HttpContext context, ICorrelationIdService correlationIdService)
    {
        if (context.Request.Headers.TryGetValue(_correlationIdHeader, out var correlationId))
        {
            correlationIdService.CorrelationId = correlationId;
            return correlationId;
        }
        else
        {
            return correlationIdService.CorrelationId;
        }
    }

    private static void AddCorrelationIdHeaderToResponse(HttpContext context, StringValues correlationId) => context.Response.OnStarting(() =>
    {
        context.Response.Headers.Add(_correlationIdHeader, new[] { correlationId.ToString() });
        return Task.CompletedTask;
    });
}
