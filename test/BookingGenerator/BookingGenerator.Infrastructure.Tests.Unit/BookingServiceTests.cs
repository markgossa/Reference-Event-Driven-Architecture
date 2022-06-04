using AspNet.CorrelationIdGenerator;
using BookingGenerator.Domain.Models;
using BookingGenerator.Infrastructure.HttpClients;
using Moq;
using System.Net.Http;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceTests
{
    private const string _failureFirstName = "Unlucky";

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsync_PostsRequestToWebBff(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();

        await MakeNewBookingAsync(mockWebBffHttpClient.Object, SetUpMockCorrelationIdGenerator(correlationId),
            booking);

        mockWebBffHttpClient.Verify(m => m.PostAsync(booking, correlationId));
    }

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncWithCorrelationId_PostsRequestToWebBffWithSameCorrelationId(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();

        await MakeNewBookingWithCorrelationIdAsync(correlationId, mockWebBffHttpClient.Object, booking);

        mockWebBffHttpClient.Verify(m => m.PostAsync(booking, correlationId));
    }

    [Theory]
    [InlineData(_failureFirstName, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failureFirstName, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async void GivenAnInstanceOfBookingService_WhenICallBookAsyncAndThereIsAnError_ThrowsException(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var correlationId = Guid.NewGuid().ToString();
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var mockWebBffHttpClient = new Mock<IWebBffHttpClient>();
        mockWebBffHttpClient.Setup(m => m.PostAsync(It.Is<Booking>(b => b.FirstName == _failureFirstName), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(async ()
            => await MakeNewBookingAsync(mockWebBffHttpClient.Object, SetUpMockCorrelationIdGenerator(correlationId), booking));
    }

    private static async Task MakeNewBookingAsync(IWebBffHttpClient webBffHttpClient,
        ICorrelationIdGenerator mockCorrelationIdGenerator, Booking booking)
            => await new BookingService(webBffHttpClient, mockCorrelationIdGenerator)
                .BookAsync(booking);

    private static Booking BuildNewBooking(string firstName, string lastName, string startDate, string endDate,
        string destination, decimal price)
            => new(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate), destination, price);

    private static ICorrelationIdGenerator SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(correlationId);

        return mockCorrelationIdGenerator.Object;
    }

    private static async Task MakeNewBookingWithCorrelationIdAsync(string correlationId,
        IWebBffHttpClient webBffHttpClient, Booking booking)
            => await new BookingService(webBffHttpClient, new CorrelationIdGenerator())
                .BookAsync(booking, correlationId);
}
