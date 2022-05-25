using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingReplayServiceTests : BookingServiceWithOutboxTestsBase
{
    [Fact]
    public async Task GivenNewInstance_WhenICallReplayBookAndSomeAreSuccessful_ThenTheMessagesAreRetrievedFromTheOutboxAndSentToTheBookingService_AndSuccessfulOnesRemoved()
    {
        var mockMessageOutbox = new Mock<IMessageOutbox<Booking>>();
        var outboxMessages = BuildOutboxMessages();
        mockMessageOutbox.Setup(m => m.GetAsync()).ReturnsAsync(outboxMessages);

        var sut = new BookingReplayService(_mockBookingService.Object, mockMessageOutbox.Object,
            new Mock<ILogger<BookingReplayService>>().Object);
        await sut.ReplayBookingsAsync();

        AssertGetsOutboxMessages(mockMessageOutbox);
        AssertOutboxMessagesAttempted(outboxMessages);
        AssertSuccessfulMessagesRemovedFromOutbox(mockMessageOutbox, GetSuccessfulOutboxMessageCorrelationIds(outboxMessages));
        var failedMessages = GetFailedOutboxMessages(outboxMessages);
        AssertFailedMessagesNotRemovedFromOutbox(mockMessageOutbox, failedMessages);
        AssertFailedMessagesSetAsFailedInOutbox(mockMessageOutbox, failedMessages);
    }
}
