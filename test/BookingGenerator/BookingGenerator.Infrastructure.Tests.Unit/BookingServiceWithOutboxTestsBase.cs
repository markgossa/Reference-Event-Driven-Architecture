using AspNet.CorrelationIdGenerator;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
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
        => mockMessageOutbox.Verify(m => m.GetAndLockAsync(It.IsAny<int>()), Times.Once);

    protected static void AssertMessageAddedToOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        Booking booking, string correlationId)
        => mockMessageOutbox.Verify(m => m.AddAsync(It.Is<Message<Booking>>(m =>
            m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    protected static void AssertFailedMessagesSetAsFailedInOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        IEnumerable<Message<Booking>> expectedFailedMessages)
    {
        foreach (var failedMessage in expectedFailedMessages)
        {
            mockMessageOutbox.Verify(m=>m.FailAsync(It.Is<IEnumerable<Message<Booking>>>(b => b.Any(b 
                => b.CorrelationId == failedMessage.CorrelationId))), Times.Once);
        }
    }

    protected static void AssertMessagesNotCompletedInOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox, IEnumerable
        <Message<Booking>> failedMessages)
    {
        mockMessageOutbox.Verify(m => m.CompleteAsync(failedMessages), Times.Never);

        foreach (var failedMessage in failedMessages)
        {
            mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>> { failedMessage }), Times.Never);
        }
    }

    protected static void AssertSuccessfulMessagesCompletedInOutbox(Mock<IMessageOutbox<Booking>> mockMessageOutbox,
        IEnumerable<Message<Booking>> messages)
    {
        foreach (var message in messages)
        {
            mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>> { message }), Times.Once);
        };
    }

    protected static Booking BuildNewBooking(string firstName, string lastName, string startDate,
        string endDate, string destination, decimal price)
            => new(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate), destination, price);

    protected static IEnumerable<Message<Booking>> BuildOutboxMessages()
        => new List<Message<Booking>>
            {
                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("Joe", "Bloggs", "10/07/2022", "25/07/2022", "Malta", 500.43m)),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking("John", "Smith", "11/07/2022", "24/07/2022", "Corfu", 305m)),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking, "Bloggs", "10/07/2022", "25/07/2022", "Malta", 500.43m)),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking, "Smith", "11/07/2022", "24/07/2022", "Corfu", 305m))
            }.AsEnumerable();

    protected static IEnumerable<Message<Booking>> GetFailedOutboxMessages(IEnumerable<Message<Booking>> outboxMessages)
        => outboxMessages.Where(m => m.MessageObject.FirstName == _failedBooking);

    protected static IEnumerable<Message<Booking>> GetSuccessfulOutboxMessages(IEnumerable<Message<Booking>> outboxMessages)
        => outboxMessages.Where(m => m.MessageObject.FirstName != _failedBooking);

    protected static Mock<ICorrelationIdGenerator> SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(correlationId);

        return mockCorrelationIdGenerator;
    }

    protected void AssertBookingNotAttempted(Booking booking)
        => _mockBookingService.Verify(m => m.BookAsync(booking, It.IsAny<string>()), Times.Never);

    protected void AssertNoMessagesCompletedInOutbox()
        => _mockMessageOutbox.Verify(m => m.CompleteAsync(It.IsAny<IEnumerable<Message<Booking>>>()), Times.Never);

    protected void AssertOutboxMessagesAttempted(IEnumerable<Message<Booking>> messages)
    {
        foreach (var message in messages)
        {
            _mockBookingService.Verify(m => m.BookAsync(message.MessageObject, message.CorrelationId));
        }
    }

    protected async Task MakeNewBookingAsync(Booking booking, Mock<ICorrelationIdGenerator> mockCorrelationIdGenerator)
        => await new BookingServiceWithOutbox(mockCorrelationIdGenerator.Object, _mockMessageOutbox.Object)
                .BookAsync(booking);

    protected void SetUpMockBookingService()
        => _mockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(b => b.FirstName == _failedBooking), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

    protected void SetUpMockMessageOutbox()
        => _mockMessageOutbox
            .Setup(m => m.AddAsync(It.Is<Message<Booking>>(b => b.MessageObject.FirstName == _failedOutboxAdd)))
            .ThrowsAsync(new Exception());

    protected void AssertMessageNotSetToFailedInOutbox(string correlationId) 
        => _mockMessageOutbox.Verify(m => m.FailAsync(
            It.Is<IEnumerable<Message<Booking>>>(s => !s.Any(t => t.CorrelationId == correlationId))),
                Times.Never);
}
