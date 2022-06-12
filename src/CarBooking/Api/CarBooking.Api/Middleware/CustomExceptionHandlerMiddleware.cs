using FluentValidation;
using LSE.Stocks.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace LSE.Stocks.Api.Middleware;

public class CustomExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public CustomExceptionHandlerMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var httpStatusCode = HttpStatusCode.InternalServerError;
        var content = string.Empty;

        switch (exception)
        {
            case ValidationException validationException:
                httpStatusCode = HttpStatusCode.BadRequest;
                content = JsonSerializer.Serialize(validationException.Errors);
                break;
            case NotFoundException _:
                httpStatusCode = HttpStatusCode.NotFound;
                break;
        }

        SetResponseProperties(context, httpStatusCode);
        await AddResponseContentAsync(context, content);
    }

    private static void SetResponseProperties(HttpContext context, HttpStatusCode httpStatusCode)
    {
        context.Response.StatusCode = (int)httpStatusCode;
        context.Response.ContentType = "application/json";
    }
    
    private static async Task AddResponseContentAsync(HttpContext context, string content) 
        => await context.Response.WriteAsync(content);
}
