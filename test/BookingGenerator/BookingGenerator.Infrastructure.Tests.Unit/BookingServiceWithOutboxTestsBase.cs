using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.CorrelationIdGenerator;
using Common.Messaging.Outbox;
using Common.Messaging.Outbox.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;

namespace BookingGenerator.Infrastructure.Tests.Unit;

public class BookingServiceWithOutboxTestsBase
{
    protected const string _failedBooking = "UnluckyBooking";
    protected const string _failedOutboxAdd = "UnluckyOutboxAdd";
    protected readonly Mock<IBookingService> _mockBookingService = new();
    protected readonly Mock<IMessageOutbox<Booking>> _mockMessageOutbox = new();

    public BookingServiceWithOutboxTestsBase()
    {
        SetUpMockBookingService();
        SetUpMockMessageOutbox();
    }

    protected static void AssertGetsOutboxMessages(Mock<IMessageOutbox<Booking>> mockMessageOutbox)
        => mockMessageOutbox.Verify(m => m.GetAsync(), Times.Once);

    protected static void AssertMessageAddedToOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        Booking booking, string correlationId)
        => mockMessageOutbox.Verify(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(m =>
            m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    protected static void AssertFailedMessagesSetAsFailedInOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        IEnumerable<OutboxMessage<Booking>> expectedFailedMessages)
            => mockMessageOutbox.Verify(m => m.FailAsync(It.Is<IEnumerable<OutboxMessage<Booking>>>(failedBookings =>
                FailedMessageCorrelationIdsMatchBookings(failedBookings, expectedFailedMessages))), Times.Once);

    protected static void AssertFailedMessagesNotRemovedFromOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox, IEnumerable
        <OutboxMessage<Booking>> failedMessages)
            => mockMessageOutbox.Verify(m => m.RemoveAsync(failedMessages.Select(m => m.CorrelationId)), Times.Never);

    protected static void AssertSuccessfulMessagesRemovedFromOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        IEnumerable<string> correlationIds)
            => mockMessageOutbox.Verify(m => m.RemoveAsync(correlationIds), Times.Once);

    protected static Booking BuildNewBooking(string firstName, string lastName, string startDate,
        string endDate, string destination, decimal price)
            => new(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate), destination, price);

    protected static IEnumerable<OutboxMessage<Booking>> BuildOutboxMessages()
        => new List<OutboxMessage<Booking>>
            {
                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("Joe", "Bloggs", "10/07/2022", "25/07/2022", "Malta", 500.43m)),

                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("John", "Smith", "11/07/2022", "24/07/2022", "Corfu", 305m)),

                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking, "Bloggs", "10/07/2022", "25/07/2022", "Malta", 500.43m)),

                new OutboxMessage<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking, "Smith", "11/07/2022", "24/07/2022", "Corfu", 305m))
            }.AsEnumerable();

    protected static bool FailedMessageCorrelationIdsMatchBookings(IEnumerable<OutboxMessage<Booking>> failedBookings,
        IEnumerable<OutboxMessage<Booking>> expectedFailedMessages)
    {
        var failedCorrelationIds = failedBookings.Select(f => f.CorrelationId).ToList();
        var expectedFailedCorrelationIds = expectedFailedMessages.Select(f => f.CorrelationId).ToList();

        if (failedCorrelationIds.Count != expectedFailedCorrelationIds.Count)
        {
            return false;
        }

        for (var i = 0; i < failedCorrelationIds.Count; i++)
        {
            if (failedCorrelationIds[i] != expectedFailedCorrelationIds[i])
            {
                return false;
            }
        }

        return true;
    }

    protected static IEnumerable<OutboxMessage<Booking>> GetFailedOutboxMessages(IEnumerable<OutboxMessage<Booking>> outboxMessages)
        => outboxMessages.Where(m => m.MessageObject.FirstName == _failedBooking);

    protected static IEnumerable<string> GetSuccessfulOutboxMessageCorrelationIds(IEnumerable<OutboxMessage<Booking>> outboxMessages)
        => outboxMessages.Where(m => m.MessageObject.FirstName != _failedBooking).Select(m => m.CorrelationId);

    protected static Mock<ICorrelationIdGenerator> SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.CorrelationId).Returns(correlationId);

        return mockCorrelationIdGenerator;
    }

    protected void AssertBookingAttempted(Booking booking)
        => _mockBookingService.Verify(m => m.BookAsync(booking, It.IsAny<string>()), Times.Once());

    protected void AssertBookingNotAttempted(Booking booking)
        => _mockBookingService.Verify(m => m.BookAsync(booking, It.IsAny<string>()), Times.Never);

    protected void AssertNoMessagesRemovedFromOutbox()
        => _mockMessageOutbox.Verify(m => m.RemoveAsync(It.IsAny<List<string>>()), Times.Never);

    protected void AssertOutboxMessagesAttempted(IEnumerable<OutboxMessage<Booking>> messages)
    {
        foreach (var message in messages)
        {
            _mockBookingService.Verify(m => m.BookAsync(message.MessageObject, message.CorrelationId));
        }
    }

    protected async Task MakeNewBookingAsync(Booking booking, Mock<ICorrelationIdGenerator> mockCorrelationIdGenerator)
        => await new BookingServiceWithOutbox(_mockBookingService.Object, mockCorrelationIdGenerator.Object,
            _mockMessageOutbox.Object, new Mock<ILogger<BookingServiceWithOutbox>>().Object)
                .BookAsync(booking);

    protected void SetUpMockBookingService()
        => _mockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(b => b.FirstName == _failedBooking), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

    protected void SetUpMockMessageOutbox()
        => _mockMessageOutbox
            .Setup(m => m.AddAsync(It.Is<OutboxMessage<Booking>>(b => b.MessageObject.FirstName == _failedOutboxAdd)))
            .ThrowsAsync(new Exception());
}
