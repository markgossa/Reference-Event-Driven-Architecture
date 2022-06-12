using AspNet.CorrelationIdGenerator;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;
using Moq;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure.Tests.Unit;
public class MessageBusOutboxTests
{
    private const string _failedOutboxAdd = "Unlucky";
    private Mock<IMessageOutbox<Booking>> _mockMessageOutbox = new();

    public MessageBusOutboxTests() => SetUpMockMessageOutbox();

    [Theory]
    [InlineData("Joe", "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData("John", "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallPublishBookingCreated_ThenMessageIsAddedToTheOutbox(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();
        await PublishBookingCreatedAsync(booking, correlationId);

        AssertMessageAddedToOutbox(booking, correlationId);
        AssertMessageNotSetToFailedInOutbox(correlationId);
        AssertMessagesNotCompletedInOutbox(new List<Message<Booking>> { new Message<Booking>(correlationId, booking) });
    }

    private async Task PublishBookingCreatedAsync(Booking booking, string correlationId)
        => await new MessageBusOutbox(_mockMessageOutbox.Object, SetUpMockCorrelationIdGenerator(correlationId).Object,
            new Mock<ILogger<MessageBusOutbox>>().Object).PublishBookingCreatedAsync(booking);

    [Theory]
    [InlineData(_failedOutboxAdd, "Bloggs", "10/06/2022", "25/06/2022", "Malta", 500.43)]
    [InlineData(_failedOutboxAdd, "Smith", "11/06/2022", "24/06/2022", "Corfu", 305)]
    public async Task GivenNewInstance_WhenICallPublishBookingCreatedAndTheMessageCouldNotBeAddedToTheOutbox_ThenThrows(
        string firstName, string lastName, string startDate, string endDate, string destination, decimal price)
    {
        var booking = BuildNewBooking(firstName, lastName, startDate, endDate, destination, price);
        var correlationId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<Exception>(async () => await PublishBookingCreatedAsync(booking, correlationId));
    }

    private void SetUpMockMessageOutbox() 
        => _mockMessageOutbox.Setup(m => m.AddAsync(It.Is<Message<Booking>>(m => m.MessageObject.FirstName.Equals(_failedOutboxAdd))))
            .ThrowsAsync(new Exception());


    protected static Booking BuildNewBooking(string firstName, string lastName, string startDate,
        string endDate, string destination, decimal price)
            => new(firstName, lastName, DateTime.Parse(startDate), DateTime.Parse(endDate), destination, price);

    protected void AssertMessageAddedToOutbox(Booking booking, string correlationId)
            => _mockMessageOutbox.Verify(m => m.AddAsync(It.Is<Message<Booking>>(m =>
                m.CorrelationId == correlationId && m.MessageObject == booking)), Times.Once);

    protected void AssertMessageNotSetToFailedInOutbox(string correlationId)
        => _mockMessageOutbox.Verify(m => m.FailAsync(It.Is<IEnumerable<Message<Booking>>>(
                s => !s.Any(t => t.CorrelationId == correlationId))), Times.Never);

    protected void AssertMessagesNotCompletedInOutbox(IEnumerable<Message<Booking>> failedMessages)
    {
        _mockMessageOutbox.Verify(m => m.CompleteAsync(failedMessages), Times.Never);

        foreach (var failedMessage in failedMessages)
        {
            _mockMessageOutbox.Verify(m => m.CompleteAsync(new List<Message<Booking>> { failedMessage }), Times.Never);
        }
    }

    protected static Mock<ICorrelationIdGenerator> SetUpMockCorrelationIdGenerator(string correlationId)
    {
        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(correlationId);

        return mockCorrelationIdGenerator;
    }
}
