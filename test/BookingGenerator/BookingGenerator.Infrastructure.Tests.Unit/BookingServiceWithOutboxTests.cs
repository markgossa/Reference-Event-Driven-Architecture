using BookingGenerator.Domain.Models;
using Common.Messaging.Folder.Models;
using Moq;
using Xunit;

namespace BookingGenerator.Infrastructure.Tests.Unit;
public class BookingServiceWithOutboxTests : BookingServiceWithOutboxTestsBase
{
    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallBookAndTheBookingIsSuccessful_ThenTheMessageIsAddedToTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await MakeNewBookingAsync(booking, SetUpMockCorrelationIdGenerator(correlationId));

        AssertMessageAddedToOutbox(_mockMessageOutbox, booking, correlationId);
        AssertBookingNotAttempted(booking);
        AssertMessageNotSetToFailedInOutbox(correlationId);
        AssertFailedMessagesNotRemovedFromOutbox(_mockMessageOutbox, 
            new List<Message<Booking>> { new Message<Booking>(correlationId, booking) });
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
}
