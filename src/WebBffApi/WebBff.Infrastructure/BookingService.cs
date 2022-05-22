using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using Common.Messaging.CorrelationIdGenerator;

namespace BookingGenerator.Infrastructure;

public class BookingService : IBookingService
{
    private readonly IWebBffHttpClient _httpClient;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;

    public BookingService(IWebBffHttpClient httpClient, ICorrelationIdGenerator correlationIdGenerator)
    {
        _httpClient = httpClient;
        _correlationIdGenerator = correlationIdGenerator;
    }

    public async Task BookAsync(Booking booking, string? correlationId = null) 
        => await _httpClient.PostAsync(booking, GetCorrelationId(correlationId));

    private string GetCorrelationId(string? correlationId) => string.IsNullOrWhiteSpace(correlationId)
                ? _correlationIdGenerator.CorrelationId
                : correlationId;
}
