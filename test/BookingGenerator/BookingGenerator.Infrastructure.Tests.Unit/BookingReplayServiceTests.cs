using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingReplayServiceTests : BookingServiceWithOutboxTestsBase
{
    [Fact]
    public async Task GivenNewInstance_WhenICallReplayBookAndSomeAreSuccessful_ThenTheMessagesAreRetrievedFromTheOutboxAndSentToTheBookingService_AndSuccessfulOnesCompleted()
    {
        var mockMessageOutbox = new Mock<IMessageOutbox<Booking>>();
        var outboxMessages = BuildOutboxMessages();
        mockMessageOutbox.Setup(m => m.GetAndLockAsync(It.IsAny<int>())).ReturnsAsync(outboxMessages);

        var sut = new BookingReplayService(_mockBookingService.Object, mockMessageOutbox.Object,
            new Mock<ILogger<BookingReplayService>>().Object);
        await sut.ReplayBookingsAsync();

        AssertGetsOutboxMessages(mockMessageOutbox);
        AssertOutboxMessagesAttempted(outboxMessages);
        AssertSuccessfulMessagesCompletedInOutbox(mockMessageOutbox, GetSuccessfulOutboxMessages(outboxMessages));
        var failedMessages = GetFailedOutboxMessages(outboxMessages);
        AssertFailedMessagesNotCompletedInOutbox(mockMessageOutbox, failedMessages);
        AssertFailedMessagesSetAsFailedInOutbox(mockMessageOutbox, failedMessages);
    }
}
