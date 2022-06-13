using CarBooking.Application.Repositories;
using CarBooking.Infrastructure.Enums;
using CarBooking.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace CarBooking.Infrastructure;

public class CarBookingSqlRepository : ICarBookingRepository
{
    private readonly CarBookingDbContext _carBookingDbContext;
    private readonly ILogger<CarBookingSqlRepository> _logger;

    public CarBookingSqlRepository(CarBookingDbContext carBookingDbContext,
        ILogger<CarBookingSqlRepository> logger)
    {
        _carBookingDbContext = carBookingDbContext;
        _logger = logger;
    }

    public async Task SaveAsync(Domain.Models.CarBooking carBooking)
    {
        try
        {
            var carBookingRow = MapToCarBookingRow(carBooking);

            await _carBookingDbContext.CarBookings.AddAsync(carBookingRow);
            await _carBookingDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (!string.IsNullOrWhiteSpace(ex.InnerException?.Message)
            && (ex.InnerException.Message.Contains("UNIQUE constraint failed")
                || ((SqlException)ex.InnerException).Number == 2601
                || ((SqlException)ex.InnerException).Number == 2627))
        {
            throw new DuplicateBookingException("Duplicate message received", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CarBookingId: {carBookingId} Error saving Car Booking to SQL", carBooking.Id);
        }
    }

    private static CarBookingRow MapToCarBookingRow(Domain.Models.CarBooking carBooking)
        => new()
        {
            CarBookingId = carBooking.Id,
            FirstName = carBooking.FirstName,
            LastName = carBooking.LastName,
            StartDate = carBooking.StartDate,
            EndDate = carBooking.EndDate,
            PickUpLocation = carBooking.PickUpLocation,
            Price = carBooking.Price,
            Size = Enum.Parse<CarSize>(carBooking.Size.ToString()),
            Transmission = Enum.Parse<CarTransmission>(carBooking.Transmission.ToString())
        };
}
