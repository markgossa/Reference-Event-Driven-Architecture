using CarBooking.Domain.Enums;
using CarBooking.Infrastructure.Enums;
using CarBooking.Infrastructure.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data.Common;

namespace CarBooking.Infrastructure.Tests.Unit;
public class CarBookingSqlRepositoryTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly CarBookingDbContext _carBookingDbContext;
    private readonly Mock<ILogger<CarBookingSqlRepository>> _mockLogger = new();

    public CarBookingSqlRepositoryTests()
    {
        var connection = CreateInMemoryDatabaseConnection();
        BuildInMemoryDatabase(connection);

        _serviceProvider = new ServiceCollection()
            .AddDbContext<CarBookingDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();

        _carBookingDbContext = _serviceProvider.GetRequiredService<CarBookingDbContext>();
    }

    [Theory]
    [InlineData("ID43792", "Jane", "Smith", 10, 15, "Corfu", 100.43, Size.Small, Transmission.Manual)]
    [InlineData("ID4424", "John", "Smith", 22, 26, "Jamaica", 200, Size.Large, Transmission.Automatic)]
    public async Task GivenNewInstance_WhenISaveACarBooking_ThenTheBookingIsSavedInTheDatabase(
        string id, string firstName, string lastName, int daysTillStartDate, int daysTillEndDate, string pickUpLocation,
        decimal price, Size size, Transmission transmission)
    {
        var carBooking = BuildCarBooking(id, firstName, lastName, daysTillStartDate, daysTillEndDate,
            pickUpLocation, price, size, transmission);

        var sut = new CarBookingSqlRepository(_carBookingDbContext, _mockLogger.Object);
        await sut.SaveAsync(carBooking);

        Assert.Single(_carBookingDbContext.CarBookings);
        AssertIsMatchingCarBooking(_carBookingDbContext.CarBookings.First(), carBooking);
    }
    
    [Fact]
    public async Task GivenNewInstance_WhenISaveTwoCarBookingsWithSameId_ThenTheBookingIsNotSavedInTheDatabaseAndThrowsDuplicateException()
    {
        var carBooking = BuildCarBooking("ID43792", "John", "Smith", 22, 26, "Jamaica", 200, Size.Large, Transmission.Automatic);

        var sut = new CarBookingSqlRepository(_carBookingDbContext, _mockLogger.Object);
        await sut.SaveAsync(carBooking);
        await Assert.ThrowsAsync<DuplicateBookingException>(async () => await sut.SaveAsync(carBooking));

        Assert.Single(_carBookingDbContext.CarBookings);
        AssertIsMatchingCarBooking(_carBookingDbContext.CarBookings.First(), carBooking);
    }

    private static Domain.Models.CarBooking BuildCarBooking(string id, string firstName, string lastName, int daysTillStartDate,
        int daysTillEndDate, string pickUpLocation, decimal price, Size size, Transmission transmission)
            => new(id, firstName, lastName, DateTime.UtcNow.AddDays(daysTillStartDate),
                DateTime.UtcNow.AddDays(daysTillEndDate), pickUpLocation, price, size, transmission);

    private static DbConnection CreateInMemoryDatabaseConnection()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        return connection;
    }

    private static void BuildInMemoryDatabase(DbConnection connection)
    {
        var options = new DbContextOptionsBuilder<CarBookingDbContext>()
            .UseSqlite(connection).Options;

        ResetSqliteDatabase(new CarBookingDbContext(options));
    }

    private static void ResetSqliteDatabase(CarBookingDbContext dbContext)
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    private static void AssertIsMatchingCarBooking(CarBookingRow carBookingRow, Domain.Models.CarBooking carBooking)
    {
        Assert.Equal(carBookingRow.CarBookingId, carBooking.Id);
        Assert.Equal(carBookingRow.FirstName, carBooking.FirstName);
        Assert.Equal(carBookingRow.LastName, carBooking.LastName);
        Assert.Equal(carBookingRow.StartDate, carBooking.StartDate);
        Assert.Equal(carBookingRow.EndDate, carBooking.EndDate);
        Assert.Equal(carBookingRow.Price, carBooking.Price);
        Assert.Equal(carBookingRow.Size, Enum.Parse<CarSize>(carBooking.Size.ToString()));
        Assert.Equal(carBookingRow.Transmission, Enum.Parse<CarTransmission>(carBooking.Transmission.ToString()));
    }
}
