using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.CorrelationIdGenerator;
using Common.Messaging.Outbox;
using Common.Messaging.Outbox.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceWithOutboxTests
{
    private const string _failedBooking = "UnluckyBooking";
    private const string _failedOutboxAdd = "UnluckyOutboxAdd";
    private readonly Mock<IBookingService> _mockBookingService = new();
    private readonly Mock<IMessageOutbox<Booking>> _mockOutboxService = new();

    public BookingServiceWithOutboxTests()
    {
        SetUpMockBookingService();
        SetUpMockMessageOutbox();
    }

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenABookingIsSuccessful_ThenTheBookingServiceIsCalledAndTheMessageIsAddedThenRemovedFromTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination,
        decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertBookingAttempted(booking);
        AssertMessageAddedToOutbox(_mockOutboxService, booking, correlationId);
        AssertMessageRemovedFromOutbox(correlationId);
    }

    [Theory]
    [InlineData(_failedBooking, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failedBooking, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenABookingFails_ThenTheBookingServiceIsCalledAndTheMessageIsAddedThenSetAsFailedInTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination,
        decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertBookingAttempted(booking);
        AssertMessageFailedInOutbox(_mockOutboxService, correlationId);
        AssertMessageNotRemovedFromOutbox();
    }
    
    [Theory]
    [InlineData(_failedOutboxAdd, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failedOutboxAdd, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenTheOutboxAddFails_ThenTheBookingServiceIsNotCalledAndThrows(
        string firstName, string lastName, string startDate, string endDate, string destination,
        decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<Exception>(async () 
            => await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId)));

        AssertBookingNotAttempted(booking);
    }
    
    private void SetUpMockBookingService()
        => _mockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(b => b.FirstName == _failedBooking)))
            .ThrowsAsync(new HttpRequestException());

    private static Booking BuildNewBooking(string firstName, string lastName, string startDate,
        string endDate, string destination, decimal price)
            => new(firstName, lastName, DateOnly.Parse(startDate), DateOnly.Parse(endDate), destination, price);

    private static Mock<ICorrelationIdGenerator> SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.CorrelationId).Returns(correlationId);

        return mockCorrelationIdGenerator;
    }

    private async Task MakeNewBookingAsync(Booking booking, Mock<ICorrelationIdGenerator> mockCorrelationIdGenerator)
        => await new BookingServiceWithOutbox(_mockBookingService.Object, mockCorrelationIdGenerator.Object,
            _mockOutboxService.Object, new Mock<ILogger<BookingServiceWithOutbox>>().Object)
                .BookAsync(booking);

    private void AssertBookingAttempted(Booking booking)
        => _mockBookingService.Verify(m => m.BookAsync(booking), Times.Once());

    private static void AssertMessageAddedToOutbox(Mock<IMessageOutbox<Booking>> mockOutboxService,
        Booking booking, string correlationId)
        => mockOutboxService.Verify(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(m =>
            m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    private void AssertMessageRemovedFromOutbox(string correlationId)
        => _mockOutboxService.Verify(m => m.RemoveAsync(correlationId), Times.Once);

    private void AssertMessageNotRemovedFromOutbox()
        => _mockOutboxService.Verify(m => m.RemoveAsync(It.IsAny<string>()), Times.Never);

    private static void AssertMessageFailedInOutbox(Mock<IMessageOutbox<Booking>> mockOutboxService, string correlationId)
            => mockOutboxService.Verify(m => m.FailAsync(correlationId), Times.Once);

    private void AssertBookingNotAttempted(Booking booking) 
        => _mockBookingService.Verify(m=>m.BookAsync(booking), Times.Never);

    private void SetUpMockMessageOutbox() 
        => _mockOutboxService
            .Setup(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(b => b.MessageObject.FirstName == _failedOutboxAdd)))
            .ThrowsAsync(new Exception());
}
