using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;
using Moq;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure.Tests.Unit;
public class MessageProcessorTests : MessageBusOutboxTestsBase
{
    private readonly Mock<ILogger<MessageProcessor>> _logger = new();
    private readonly string _failedBooking = "Unlucky";

    [Fact]
    public async Task GivenNewInstance_WhenABookingCreatedMessageIsPublishedSuccessfully_ThenSetsMessageAsCompletedInOutbox()
    {
        var mockMessageBus = SetUpMockMessageBus();
        var outboxMessages = BuildOutboxMessages();
        var mockMessageOutbox = SetUpMockMessageOutbox(outboxMessages);

        var sut = new MessageProcessor(mockMessageBus.Object, mockMessageOutbox.Object, _logger.Object);
        await sut.PublishBookingCreatedMessagesAsync();

        AssertAttemptsToPublishBookingCreatedMessages(mockMessageBus, outboxMessages);
        AssertSuccessfulMessagesSetAsCompletedInOutbox(outboxMessages, mockMessageOutbox);
        AssertFailedMessagesSetAsFailedInOutbox(outboxMessages, mockMessageOutbox);
    }

    private void AssertSuccessfulMessagesSetAsCompletedInOutbox(IEnumerable<Message<Booking>> outboxMessages, Mock<IMessageOutbox<Booking>> mockMessageOutbox)
    {
        foreach (var outboxMessage in outboxMessages.Where(m => !m.MessageObject.BookingSummary.FirstName.Equals(_failedBooking)))
        {
            mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>>() { outboxMessage }), Times.Once);
        }
    }
    
    private void AssertFailedMessagesSetAsFailedInOutbox(IEnumerable<Message<Booking>> outboxMessages, Mock<IMessageOutbox<Booking>> mockMessageOutbox)
    {
        foreach (var outboxMessage in outboxMessages.Where(m => m.MessageObject.BookingSummary.FirstName.Equals(_failedBooking)))
        {
            mockMessageOutbox.Verify(m => m.FailAsync(new List<Message<Booking>>() { outboxMessage }), Times.Once);
            mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>>() { outboxMessage }), Times.Never);
        }
    }

    private Mock<IMessageBus> SetUpMockMessageBus()
    {
        var mockMessageBus = new Mock<IMessageBus>();
        mockMessageBus.Setup(m => m.PublishBookingCreatedAsync(It.Is<Booking>(b => b.BookingSummary.FirstName.Equals(_failedBooking)))).ThrowsAsync(new Exception());
        return mockMessageBus;
    }

    private static Mock<IMessageOutbox<Booking>> SetUpMockMessageOutbox(IEnumerable<Message<Booking>> outboxMessages)
    {
        var mockMessageOutbox = new Mock<IMessageOutbox<Booking>>();
        mockMessageOutbox.Setup(m => m.GetAndLockAsync(4)).ReturnsAsync(outboxMessages);
        return mockMessageOutbox;
    }

    private static void AssertAttemptsToPublishBookingCreatedMessages(Mock<IMessageBus> mockMessageBus, 
        IEnumerable<Message<Booking>> outboxMessages)
    {
        foreach (var outboxMessage in outboxMessages)
        {
            mockMessageBus.Verify(m => m.PublishBookingCreatedAsync(outboxMessage.MessageObject), Times.Once);
        }
    }

    private IEnumerable<Message<Booking>> BuildOutboxMessages()
        => new List<Message<Booking>>
            {
                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking()),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking()),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking)),

                new Message<Booking>(Guid.NewGuid().ToString(),
                    BuildNewBooking(_failedBooking))
            }.AsEnumerable();
}
