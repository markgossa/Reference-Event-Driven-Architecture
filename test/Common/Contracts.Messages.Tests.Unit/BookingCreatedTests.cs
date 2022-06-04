using System.Text.Json;

namespace Contracts.Messages.Tests.Unit;

public class BookingCreatedTests
{
    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "22/06/2022", "Paris", 400)]
    [InlineData("Mary", "Jane", "15/06/2022", "30/06/2022", "Paraguay", 400)]
    public void GivenNewInstance_WhenCreated_ThenCanSetTheCorrectProperties(string firstName,
        string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var sut = new BookingCreated(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate),
            destination, price);

        Assert.Equal(firstName, sut.FirstName);
        Assert.Equal(lastName, sut.LastName);
        Assert.Equal(DateTime.Parse(startDate), sut.StartDate);
        Assert.Equal(DateTime.Parse(endDate), sut.EndDate);
        Assert.Equal(destination, sut.Destination);
        Assert.Equal(price, sut.Price);
    }

    [Theory]
    [InlineData("Mary", "Jane", "15/06/2022", "30/06/2022", "Paraguay", 400, @"Data\BookingCreated1.json")]
    [InlineData("Joe", "Bloggs", "10/06/2022", "22/06/2022", "Paris", 400, @"Data\BookingCreated2.json")]
    public async Task GivenValidJson_WhenDeserialized_ThenTheCorrectPropertiesAreSet(string firstName,
        string lastName, string startDate, string endDate, string destination, decimal price, string jsonFilePath)
    {
        var json = await File.ReadAllTextAsync(jsonFilePath);
        var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(json);

        Assert.Equal(firstName, bookingCreated?.FirstName);
        Assert.Equal(lastName, bookingCreated?.LastName);
        Assert.Equal(DateTime.Parse(startDate), bookingCreated?.StartDate);
        Assert.Equal(DateTime.Parse(endDate), bookingCreated?.EndDate);
        Assert.Equal(destination, bookingCreated?.Destination);
        Assert.Equal(price, bookingCreated?.Price);
    }
}