using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Common.Messaging.Folder.Repositories;
using Common.Messaging.Outbox.Tests.Unit.Models;
using Moq;
using Xunit;

namespace Common.Messaging.Outbox.Tests.Unit;
public class MessageOutboxTests
{
    private readonly Mock<IMessageRepository<PhoneCall>> _mockOutboxMessageRepository = new();

    [Theory]
    [InlineData("0123456789", "9876543210")]
    [InlineData("9876543210", "0123456789")]
    public async Task GivenANewInstance_WhenAnOutboxMessageIsAdded_ThenTheItemIsAddedToTheRepositoryAndMessagesAreLocked(
        string source, string destination)
    {
        Message<PhoneCall>? actualOutboxMessage = null;
        _mockOutboxMessageRepository.Setup(m => m.AddAsync(It.IsAny<Message<PhoneCall>>()))
            .Callback<Message<PhoneCall>>(o => actualOutboxMessage = o);
        var outboxMessage = new Message<PhoneCall>(Guid.NewGuid().ToString(), new PhoneCall(source, destination));

        var sut = new MessageFolder<PhoneCall>(_mockOutboxMessageRepository.Object);
        await sut.AddAsync(outboxMessage);

        Assert.Equal(outboxMessage.CorrelationId, actualOutboxMessage?.CorrelationId);
        Assert.NotNull(outboxMessage.MessageObject);
        Assert.Equal(outboxMessage.MessageObject.Source, actualOutboxMessage?.MessageObject.Source);
        Assert.Equal(outboxMessage.MessageObject.Destination, actualOutboxMessage?.MessageObject.Destination);
        Assert.Equal(0, actualOutboxMessage?.AttemptCount);
        Assert.Null(actualOutboxMessage?.LastAttempt);
        Assert.Null(actualOutboxMessage?.LockExpiry);
    }
    
    [Fact]
    public async Task GivenANewInstance_WhenOutboxMessagesAreCompleted_ThenTheItemsAreCompletedInTheRepository()
    {
        var correlationIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        var sut = new MessageFolder<PhoneCall>(_mockOutboxMessageRepository.Object);
        await sut.CompleteAsync(correlationIds);

        _mockOutboxMessageRepository.Verify(m=>m.CompleteAsync(correlationIds), Times.Once());
    }
    
    [Fact]
    public async Task GivenANewInstance_WhenOutboxMessagesAreRetrieved_ThenTheItemsAreRetrievedFromTheRepositoryAndMessagesAreLocked()
    {
        var expectedOutboxMessages = BuildOutboxMessages();
        _mockOutboxMessageRepository.Setup(m => m.GetAsync()).Returns(Task.FromResult(expectedOutboxMessages));
        var sut = new MessageFolder<PhoneCall>(_mockOutboxMessageRepository.Object);
        var outboxMessages = await sut.GetAsync();

        Assert.Equal(expectedOutboxMessages, outboxMessages);
    }

    [Fact]
    public async Task GivenANewInstance_WhenOutboxMessagesAreFailed_ThenTheItemsAreFailedInTheRepositoryAndAttemptCountAndLastUpdateTimeUpdated()
    {
        var failedMessages = BuildOutboxMessages();
        List<Message<PhoneCall>>? actualFailedMessages = null;
        _mockOutboxMessageRepository.Setup(m => m.UpdateAsync(failedMessages))
            .Callback<IEnumerable<Message<PhoneCall>>>(m => actualFailedMessages = m.ToList());

        var sut = new MessageFolder<PhoneCall>(_mockOutboxMessageRepository.Object);
        await sut.FailAsync(failedMessages);

        _mockOutboxMessageRepository.Verify(m => m.UpdateAsync(failedMessages), Times.Once);

        Assert.Equal(1, actualFailedMessages?[0].AttemptCount);
        Assert.Equal(2, actualFailedMessages?[1].AttemptCount);
        Assert.True(failedMessages.All(m => IsDateTimeNow(m.LastAttempt)));
        Assert.True(failedMessages.All(m => m.LockExpiry is null));
    }
    
    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 2000)]
    [InlineData(2, 4000)]
    public async Task GivenANewInstance_WhenOutboxMessagesAreFailed_ThenTheItemsAreFailedInTheRepositoryAndRetryAfterSetToExponentialBackoff(
        int initialAttemptCount, int millisecondsTillFirstRetry)
    {
        var failedMessages = new List<Message<PhoneCall>>
        {
            new (Guid.NewGuid().ToString(), new PhoneCall("0123456789", "9876543210"))
                {
                    LockExpiry = null,
                    LastAttempt = null,
                    AttemptCount = initialAttemptCount
                }
        };

        List<Message<PhoneCall>>? actualFailedMessages = null;
        _mockOutboxMessageRepository.Setup(m => m.UpdateAsync(failedMessages))
            .Callback<IEnumerable<Message<PhoneCall>>>(m => actualFailedMessages = m.ToList());

        var sut = new MessageFolder<PhoneCall>(_mockOutboxMessageRepository.Object);
        await sut.FailAsync(failedMessages);

        _mockOutboxMessageRepository.Verify(m => m.UpdateAsync(failedMessages), Times.Once);

        Assert.True(IsDateTimeNow(actualFailedMessages?.First().RetryAfter, addMilliseconds: millisecondsTillFirstRetry));
    }

    private static IEnumerable<Message<PhoneCall>> BuildOutboxMessages()
        => new List<Message<PhoneCall>>()
        { 
                new (Guid.NewGuid().ToString(), new PhoneCall("0123456789", "9876543210"))
                {
                    LockExpiry = null,
                    LastAttempt = null
                },
                new (Guid.NewGuid().ToString(), new PhoneCall("9876543210", "0123456789"))
                {
                    LockExpiry = DateTime.UtcNow.AddMinutes(-8),
                    LastAttempt = DateTime.UtcNow.AddMinutes(-9),
                    RetryAfter = DateTime.UtcNow.AddMinutes(-2),
                    AttemptCount = 1,
                }
        }.AsEnumerable();

    private static bool IsDateTimeNow(DateTime? actualDateTime, int addMilliseconds = 0)
    {
        var expectedDateTime = DateTime.UtcNow.AddMilliseconds(addMilliseconds);
        var bufferInMilliseconds = 100;

        return actualDateTime > expectedDateTime.AddMilliseconds(-bufferInMilliseconds)
            && actualDateTime < expectedDateTime.AddMilliseconds(bufferInMilliseconds);
    }
}