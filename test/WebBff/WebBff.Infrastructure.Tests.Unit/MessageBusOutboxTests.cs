using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;
using Moq;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure.Tests.Unit;
public class MessageBusOutboxTests : MessageBusOutboxTestsBase
{
    private const string _failedOutboxAdd = "Unlucky";
    private readonly Mock<IMessageOutbox<Booking>> _mockMessageOutbox = new();

    public MessageBusOutboxTests() => SetUpMockMessageOutbox();

    [Fact]
    public async Task GivenNewInstance_WhenICallPublishBookingCreated_ThenMessageIsAddedToTheOutbox()
    {
        var booking = BuildNewBooking();
        var correlationId = booking.BookingId;
        await PublishBookingCreatedAsync(booking);

        AssertMessageAddedToOutbox(booking, correlationId);
        AssertMessageNotSetToFailedInOutbox(correlationId);
        AssertMessagesNotCompletedInOutbox(new List<Message<Booking>> { new Message<Booking>(correlationId, booking) });
    }

    private async Task PublishBookingCreatedAsync(Booking booking)
        => await new MessageBusOutbox(_mockMessageOutbox.Object, new Mock<ILogger<MessageBusOutbox>>().Object)
            .PublishBookingCreatedAsync(booking);

    [Fact]
    public async Task GivenNewInstance_WhenICallPublishBookingCreatedAndTheMessageCouldNotBeAddedToTheOutbox_ThenThrows()
    {
        var booking = BuildNewBooking(_failedOutboxAdd);
        var correlationId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<Exception>(async () => await PublishBookingCreatedAsync(booking));
    }

    private void SetUpMockMessageOutbox() 
        => _mockMessageOutbox.Setup(m => m.AddAsync(It.Is<Message<Booking>>(m 
                => m.MessageObject.BookingSummary.FirstName.Equals(_failedOutboxAdd))))
            .ThrowsAsync(new Exception());

    private void AssertMessageAddedToOutbox(Booking booking, string correlationId)
            => _mockMessageOutbox.Verify(m => m.AddAsync(It.Is<Message<Booking>>(m =>
                m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    private void AssertMessageNotSetToFailedInOutbox(string correlationId)
        => _mockMessageOutbox.Verify(m => m.FailAsync(It.Is<IEnumerable<Message<Booking>>>(
                s => !s.Any(t => t.CorrelationId == correlationId))), Times.Never);

    private void AssertMessagesNotCompletedInOutbox(IEnumerable<Message<Booking>> failedMessages)
    {
        _mockMessageOutbox.Verify(m => m.CompleteAsync(failedMessages), Times.Never);

        foreach (var failedMessage in failedMessages)
        {
            _mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>> { failedMessage }), Times.Never);
        }
    }
}
