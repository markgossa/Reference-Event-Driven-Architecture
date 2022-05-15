using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;

namespace BookingGenerator.Infrastructure;

public class BookingService : IBookingService
{
    private readonly IWebBffHttpClient _httpClient;

    public BookingService(IWebBffHttpClient httpClient) => _httpClient = httpClient;

    public async Task BookAsync(Booking booking) => await _httpClient.PostAsync(booking);
}
