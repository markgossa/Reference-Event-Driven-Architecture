using Common.Messaging.Folder.Models;
using Common.Messaging.Repository.Sql.Models;
using Common.Messaging.Outbox.Sql.Tests.Unit.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Text.Json;
using Xunit;
using Common.Messaging.Repository.Sql;
using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Messaging.Outbox.Sql.Tests.Unit;
public class SqlMessageRepositoryTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MessageDbContext _messageDbContext;
    private const int _lockExpirySeconds = 30;
    private readonly Mock<ILogger<SqlMessageRepository<Tree>>> _logger = new();

    public SqlMessageRepositoryTests()
    {
        var connection = CreateInMemoryDatabaseConnection();
        BuildInMemoryDatabase(connection);

        _serviceProvider = new ServiceCollection()
            .AddDbContext<MessageDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();
        _messageDbContext = _serviceProvider.GetRequiredService<MessageDbContext>();
    }

    [Fact]
    public async Task GivenNewInstance_WhenAMessageIsAdded_ThenMessageIsSavedInSqlAndMessageObjectSerializedToJson()
    {
        var messageToAdd = new Message<Tree>(Guid.NewGuid().ToString(), new Tree("Sycamore", 5))
        {
            AttemptCount = 1,
            LastAttempt = DateTime.UtcNow.AddDays(-1),
            LockExpiry = DateTime.UtcNow.AddDays(-1)
        };

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        await sut.AddAsync(messageToAdd);

        AssertMessagePropertiesSavedToDatabase(messageToAdd);
    }

    [Theory]
    [InlineData("15/07/2022 15:03", "15/07/2022 15:02", 2, "16/07/2022 15:30")]
    [InlineData(null, null, 5, null)]
    public async Task GivenNewInstance_WhenAMessageIsUpdated_ThenTheMessageIsUpdatedInSql(string lockExpiry,
        string lastAttempt, int attemptCount, string retryAfter)
    {
        var message = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(message);

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messageToUpdate = new Message<Tree>(message.CorrelationId, JsonSerializer.Deserialize<Tree>(message.MessageBlob)!)
        {
            LockExpiry = ParseDate(lockExpiry),
            LastAttempt = ParseDate(lastAttempt),
            AttemptCount = attemptCount,
            RetryAfter = ParseDate(retryAfter)
        };

        await sut.UpdateAsync(new List<Message<Tree>> { messageToUpdate });

        AssertMessagePropertiesSavedToDatabase(messageToUpdate);
    }
    
    [Fact]
    public async Task GivenNewInstance_WhenAMessageIsUpdatedAndIsThenRemovedFromSql_ThenDoesNotThrow()
    {
        var missingMessage = BuildMessageSqlRow();

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var missingMessageToUpdate = new Message<Tree>(missingMessage.CorrelationId, JsonSerializer.Deserialize<Tree>(missingMessage.MessageBlob)!)
        {
            LockExpiry = DateTime.UtcNow.AddMinutes(-10),
            LastAttempt = DateTime.UtcNow.AddMinutes(-10),
            AttemptCount = 5,
            RetryAfter = DateTime.UtcNow.AddMinutes(-10)
        };

        await sut.UpdateAsync(new List<Message<Tree>> { missingMessageToUpdate });
    }

    [Fact]
    public async Task GivenNewInstance_WhenAMessageIsRemoved_ThenTheMessageIsRemovedFromSql()
    {
        var message = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(message);

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        await sut.RemoveAsync(new List<string> { message.CorrelationId });

        Assert.Empty(_messageDbContext.Messages);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLockMessages_ThenMessagesWithANullLockExpiryAreReturnedWithAllPropertiesAndMessagesAreLocked()
    {
        var outboxMessageRow1 = BuildMessageSqlRow();
        var outboxMessageRow2 = BuildMessageSqlRow(lockExpiry: DateTime.UtcNow.AddMinutes(2));
        await AddMessageToDatabaseAsync(outboxMessageRow1);
        await AddMessageToDatabaseAsync(outboxMessageRow2);
     
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(10);

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow1, messages.Single(), isMessageLocked: true);
        AssertMessageSetAsLockedInSql(outboxMessageRow1);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLockMessages_ThenMessagesWithAnExpiredLockAreReturnedWithAllPropertiesAndMessagesAreLocked()
    {
        var outboxMessageRow1 = BuildMessageSqlRow(DateTime.UtcNow.AddSeconds(-1));
        var outboxMessageRow2 = BuildMessageSqlRow(DateTime.UtcNow.AddSeconds(1));
        await AddMessageToDatabaseAsync(outboxMessageRow1);
        await AddMessageToDatabaseAsync(outboxMessageRow2);
     
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(10);

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow1, messages.FirstOrDefault(), isMessageLocked: true);
        AssertMessageSetAsLockedInSql(outboxMessageRow1);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLockMessages_ThenMessagesWithARetryAfterTimeAfterUtcNowAreReturnedWithAllPropertiesAndMessagesAreLocked()
    {
        var outboxMessageRow1 = BuildMessageSqlRow(null, DateTime.UtcNow.AddSeconds(-1));
        var outboxMessageRow2 = BuildMessageSqlRow(null, DateTime.UtcNow.AddSeconds(1));
        await AddMessageToDatabaseAsync(outboxMessageRow1);
        await AddMessageToDatabaseAsync(outboxMessageRow2);
        
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(10);

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow1, messages.FirstOrDefault(), isMessageLocked: true);
        AssertMessageSetAsLockedInSql(outboxMessageRow1);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLockMessages_ThenMessagesWithANullRetryAfterTimeAreReturnedWithAllPropertiesAndMessagesAreLocked()
    {
        var outboxMessageRow = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(outboxMessageRow);
        
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(10);

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow, messages.FirstOrDefault(), isMessageLocked: true);
        AssertMessageSetAsLockedInSql(outboxMessageRow);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLock1Messages_ThenTheMessageWithOldestRetryAfterTimeIsReturned()
    {
        var outboxMessageRow1 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-1));
        var outboxMessageRow2 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-2));
        var outboxMessageRow3 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-3));
        await AddMessageToDatabaseAsync(outboxMessageRow1);
        await AddMessageToDatabaseAsync(outboxMessageRow2);
        await AddMessageToDatabaseAsync(outboxMessageRow3);

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(1);

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow3, messages.FirstOrDefault(), isMessageLocked: true);  
    }
    
    [Fact]
    public async Task GivenNewInstance_WhenIGetAndLock2Messages_ThenTheMessagesWithOldestRetryAfterTimeAreReturned()
    {
        var outboxMessageRow1 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-1));
        var outboxMessageRow2 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-2));
        var outboxMessageRow3 = BuildMessageSqlRow(null, DateTime.UtcNow.AddMinutes(-3));
        await AddMessageToDatabaseAsync(outboxMessageRow1);
        await AddMessageToDatabaseAsync(outboxMessageRow2);
        await AddMessageToDatabaseAsync(outboxMessageRow3);

        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var messages = await sut.GetAndLockAsync(2);

        Assert.Equal(2, messages.Count());
        AssertPropertiesReturned(outboxMessageRow3, messages.FirstOrDefault(), isMessageLocked: true);  
        AssertPropertiesReturned(outboxMessageRow2, messages.LastOrDefault(), isMessageLocked: true);  
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithANullLockExpiryAreReturnedWithAllProperties()
    {
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var outboxMessageRow = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(outboxMessageRow);
        var messages = await sut.GetAsync();

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow, messages.Single());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithAnExpiredLockAreReturnedWithAllProperties(
        int secondsTillLockExpires, int expectedMessageCount)
    {
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var outboxMessageRow = BuildMessageSqlRow(DateTime.UtcNow.AddSeconds(secondsTillLockExpires));
        await AddMessageToDatabaseAsync(outboxMessageRow);
        var messages = await sut.GetAsync();

        Assert.Equal(expectedMessageCount, messages.Count());
        AssertPropertiesReturned(outboxMessageRow, messages.FirstOrDefault());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithARetryAfterTimeAfterUtcNowAreReturnedWithAllProperties(
        int secondsTillRetryAfterElapses, int expectedMessageCount)
    {
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var outboxMessageRow = BuildMessageSqlRow(null,
                    DateTime.UtcNow.AddSeconds(secondsTillRetryAfterElapses));
        await AddMessageToDatabaseAsync(outboxMessageRow);
        var messages = await sut.GetAsync();

        Assert.Equal(expectedMessageCount, messages.Count());
        AssertPropertiesReturned(outboxMessageRow, messages.FirstOrDefault());
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithANullRetryAfterTimeAreReturnedWithAllProperties()
    {
        var sut = new SqlMessageRepository<Tree>(_messageDbContext, _logger.Object);
        var outboxMessageRow = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(outboxMessageRow);
        var messages = await sut.GetAsync();

        Assert.Single(messages);
        AssertPropertiesReturned(outboxMessageRow, messages.FirstOrDefault());
    }

    private static DateTime? ParseDate(string date)
        => !string.IsNullOrWhiteSpace(date) ? DateTime.Parse(date) : null;

    private static DbConnection CreateInMemoryDatabaseConnection()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        return connection;
    }

    private static void BuildInMemoryDatabase(DbConnection connection)
    {
        var options = new DbContextOptionsBuilder<MessageDbContext>()
            .UseSqlite(connection).Options;
        ResetSqliteDatabase(new MessageDbContext(options));
    }

    private static void ResetSqliteDatabase(MessageDbContext dbContext)
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    private void AssertMessagePropertiesSavedToDatabase(Message<Tree> messageToAdd)
    {
        var savedMessages = _messageDbContext.Messages;
        Assert.Single(savedMessages);
        Assert.Single(savedMessages.Where(m => m.CorrelationId == messageToAdd.CorrelationId));
        Assert.Single(savedMessages.Where(m => m.LastAttempt == messageToAdd.LastAttempt));
        Assert.Single(savedMessages.Where(m => m.AttemptCount == messageToAdd.AttemptCount));
        Assert.Single(savedMessages.Where(m => m.LockExpiry == messageToAdd.LockExpiry));
        Assert.Single(savedMessages.Where(m => m.RetryAfter == messageToAdd.RetryAfter));
        Assert.Single(savedMessages.Where(m => m.MessageBlob == JsonSerializer.Serialize(messageToAdd.MessageObject,
            new JsonSerializerOptions())));
    }

    private async Task AddMessageToDatabaseAsync(MessageSqlRow message)
    {
        await _messageDbContext.Messages.AddAsync(message);
        await _messageDbContext.SaveChangesAsync();
    }

    private static MessageSqlRow BuildMessageSqlRow(DateTime? lockExpiry = null,
        DateTime? retryAfter = null)
        => new()
        {
            AttemptCount = new Random().Next(0,100),
            CorrelationId = Guid.NewGuid().ToString(),
            LastAttempt = DateTime.UtcNow.AddDays(-1),
            MessageBlob = JsonSerializer.Serialize(new Tree("Sycamore", 5)),
            LockExpiry = lockExpiry,
            RetryAfter = retryAfter
        };

    private static void AssertPropertiesReturned(MessageSqlRow outboxMessageRow, Message<Tree>? outboxMessage,
        bool isMessageLocked = false)
    {
        if (outboxMessage is not null)
        {
            AssertNonLockExpiryProperties(outboxMessageRow, outboxMessage);

            AssertMessageLockExpiry(outboxMessageRow, outboxMessage, isMessageLocked);

            Assert.Equal(outboxMessageRow.RetryAfter, outboxMessage.RetryAfter);
        }
    }

    private static void AssertMessageLockExpiry(MessageSqlRow outboxMessageRow, Message<Tree>? outboxMessage, bool isMessageLocked)
    {
        if (isMessageLocked)
        {
            var lockExpiryTime = DateTime.UtcNow.AddSeconds(_lockExpirySeconds);
            AssertLockExpiryTime(outboxMessage?.LockExpiry, lockExpiryTime);
        }
        else
        {
            Assert.Equal(outboxMessageRow.LockExpiry, outboxMessage?.LockExpiry);
        }
    }

    private static void AssertNonLockExpiryProperties(MessageSqlRow outboxMessageRow, Message<Tree>? outboxMessage)
    {
        Assert.Equal(outboxMessageRow.MessageBlob, JsonSerializer.Serialize(outboxMessage?.MessageObject));
        Assert.Equal(outboxMessageRow.CorrelationId, outboxMessage?.CorrelationId);
        Assert.Equal(outboxMessageRow.AttemptCount, outboxMessage?.AttemptCount);
        Assert.Equal(outboxMessageRow.LastAttempt, outboxMessage?.LastAttempt);
    }

    private static void AssertLockExpiryTime(DateTime? actualLockExpiry, DateTime expectedLockExpiry) 
        => Assert.InRange(actualLockExpiry ?? DateTime.MinValue, expectedLockExpiry.AddMilliseconds(-100),
            expectedLockExpiry.AddMilliseconds(100));

    private void AssertMessageSetAsLockedInSql(MessageSqlRow outboxMessageRow1)
    {
        var savedMessages = _messageDbContext.Messages.Where(m => m.CorrelationId == outboxMessageRow1.CorrelationId);
        AssertLockExpiryTime(savedMessages.First().LockExpiry, DateTime.UtcNow.AddSeconds(_lockExpirySeconds));
    }
}
