using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Common.CorrelationIdGenerator;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string _correlationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        var correlationId = GetCorrelationId(context, correlationIdGenerator);
        AddCorrelationIdHeaderToResponse(context, correlationId);

        await _next(context);
    }

    private static StringValues GetCorrelationId(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        if (context.Request.Headers.TryGetValue(_correlationIdHeader, out var correlationId))
        {
            correlationIdGenerator.CorrelationId = correlationId;
            return correlationId;
        }
        else
        {
            return correlationIdGenerator.CorrelationId;
        }
    }

    private static void AddCorrelationIdHeaderToResponse(HttpContext context, StringValues correlationId) => context.Response.OnStarting(() =>
    {
        context.Response.Headers.Add(_correlationIdHeader, new[] { correlationId.ToString() });
        return Task.CompletedTask;
    });
}
