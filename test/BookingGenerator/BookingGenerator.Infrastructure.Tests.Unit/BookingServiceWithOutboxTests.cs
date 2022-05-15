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
    private readonly Mock<IMessageOutbox<Booking>> _mockMessageOutbox = new();

    public BookingServiceWithOutboxTests()
    {
        SetUpMockBookingService();
        SetUpMockMessageOutbox();
    }

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallBookAndTheBookingIsSuccessful_ThenTheBookingServiceIsCalledAndTheMessageIsAddedThenRemovedFromTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertBookingAttempted(booking);
        AssertMessageAddedToOutbox(_mockMessageOutbox, booking, correlationId);
        AssertMessageRemovedFromOutbox(correlationId);
    }

    [Theory]
    [InlineData(_failedBooking, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failedBooking, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallBookAndTheBookingFails_ThenTheBookingServiceIsCalledAndTheMessageIsAddedThenSetAsFailedInTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertBookingAttempted(booking);
        AssertMessageFailedInOutbox(_mockMessageOutbox, correlationId);
        AssertMessageNotRemovedFromOutbox();
    }

    [Theory]
    [InlineData(_failedOutboxAdd, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failedOutboxAdd, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallBookAndTheOutboxAddFails_ThenTheBookingServiceIsNotCalledAndThrows(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<Exception>(async ()
            => await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId)));

        AssertBookingNotAttempted(booking);
    }

    [Fact]
    public async Task GivenNewInstance_WhenICallReplayBook_ThenTheMessagesAreRetrievedFromTheOutboxAndSentToTheBookingService_AndRemoved()
    {
        var mockMessageOutbox = new Mock<IMessageOutbox<Booking>>();
        var outboxMessages = BuildOutboxMessages();
        mockMessageOutbox.Setup(m => m.GetAsync()).ReturnsAsync(outboxMessages);

        var sut = new BookingServiceWithOutbox(_mockBookingService.Object, new Mock<ICorrelationIdGenerator>().Object,
            mockMessageOutbox.Object, new Mock<ILogger<BookingServiceWithOutbox>>().Object);
        await sut.ReplayBookingsAsync();
        
        AssertGetsOutboxMessages(mockMessageOutbox);
        AssertOutboxMessagesAttempted(outboxMessages);
        AssertMessagesRemovedFromOutbox(mockMessageOutbox, outboxMessages.Select(m => m.CorrelationId));
    }

    private void SetUpMockBookingService()
        => _mockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(b => b.FirstName == _failedBooking), It.IsAny<string>()))
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

    private void SetUpMockMessageOutbox()
        => _mockMessageOutbox
            .Setup(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(b => b.MessageObject.FirstName == _failedOutboxAdd)))
            .ThrowsAsync(new Exception());

    private async Task MakeNewBookingAsync(Booking booking, Mock<ICorrelationIdGenerator> mockCorrelationIdGenerator)
        => await new BookingServiceWithOutbox(_mockBookingService.Object, mockCorrelationIdGenerator.Object,
            _mockMessageOutbox.Object, new Mock<ILogger<BookingServiceWithOutbox>>().Object)
                .BookAsync(booking);

    private void AssertBookingAttempted(Booking booking) 
        => _mockBookingService.Verify(m => m.BookAsync(booking, It.IsAny<string>()), Times.Once());

    private static void AssertMessageAddedToOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        Booking booking, string correlationId)
        => mockMessageOutbox.Verify(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(m =>
            m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    private static void AssertMessagesRemovedFromOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox, 
        IEnumerable<string> correlationIds)
    {
        foreach (var correlationId in correlationIds)
        {
            mockMessageOutbox.Verify(m => m.RemoveAsync(correlationId), Times.Once);
        }
    }

    private void AssertMessageRemovedFromOutbox(string correlationId)
        => _mockMessageOutbox.Verify(m => m.RemoveAsync(correlationId), Times.Once);

    private void AssertMessageNotRemovedFromOutbox()
        => _mockMessageOutbox.Verify(m => m.RemoveAsync(It.IsAny<string>()), Times.Never);

    private static void AssertMessageFailedInOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox, string correlationId)
            => mockMessageOutbox.Verify(m => m.FailAsync(correlationId), Times.Once);

    private void AssertBookingNotAttempted(Booking booking)
        => _mockBookingService.Verify(m => m.BookAsync(booking, It.IsAny<string>()), Times.Never);

    private static IEnumerable<OutboxMessage<Booking>> BuildOutboxMessages()
        => new List<OutboxMessage<Booking>>
            {
                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("Joe", "Bloggs", "10/07/2022", "25/07/2022", "Malta", 500.43m)),
                
                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("John", "Smith", "11/07/2022", "24/07/2022", "Corfu", 305m))
            }.AsEnumerable();

    private void AssertOutboxMessagesAttempted(IEnumerable<OutboxMessage<Booking>> messages)
    {
        foreach (var message in messages)
        {
            _mockBookingService.Verify(m => m.BookAsync(message.MessageObject, message.CorrelationId));
        }
    }

    private static void AssertGetsOutboxMessages(Mock<IMessageOutbox<Booking>> mockMessageOutbox)
        => mockMessageOutbox.Verify(m => m.GetAsync(), Times.Once);
}
