using BookingGenerator.Domain.Models;
using Common.Messaging.Folder.Models;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceWithOutboxTests : BookingServiceWithOutboxTestsBase
{
    [Fact]
    public async Task GivenNewInstance_WhenICallBook_ThenTheMessageIsAddedToTheOutbox()
    {
        var booking = BuildNewBooking();
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertMessageAddedToOutbox(_mockMessageOutbox, booking, correlationId);
        AssertBookingNotAttempted(booking);
        AssertMessageNotSetToFailedInOutbox(correlationId);
        AssertMessagesNotCompletedInOutbox(_mockMessageOutbox, 
            new List<Message<Booking>> { new Message<Booking>(correlationId, booking) });
    }

    [Fact]
    public async Task GivenNewInstance_WhenICallBookAndTheOutboxAddFails_ThenThrows()
    {
        var booking = BuildNewBooking(_failedOutboxAdd);
        var correlationId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<Exception>(async ()
            => await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId)));

        AssertBookingNotAttempted(booking);
    }
}
