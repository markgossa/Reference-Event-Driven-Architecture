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

namespace Common.Messaging.Outbox.Sql.Tests.Unit;
public class SqlMessageRepositoryTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MessageDbContext _messageDbContext;

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

        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
        await sut.AddAsync(messageToAdd);

        AssertMessagePropertiesSavedToDatabase(messageToAdd);
    }

    [Theory]
    [InlineData("15/07/2022 15:03", "15/07/2022 15:02", 2, "16/07/2022 15:30")]
    [InlineData(null, null, 5, null)]
    public async Task GivenNewInstance_WhenAMessageIsUpdated_ThenMessageIsUpdatedInSql(string lockExpiry,
        string lastAttempt, int attemptCount, string retryAfter)
    {
        var message = BuildMessageSqlRow();
        await AddMessageToDatabaseAsync(message);

        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
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
    public async Task GivenNewInstance_WhenAMessageIsRemoved_ThenMessageIsRemovedFromSql()
    {
        var message = BuildMessageSqlRow();

        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
        await AddMessageToDatabaseAsync(message);
        await sut.RemoveAsync(new List<string> { message.CorrelationId });

        Assert.Empty(_messageDbContext.Messages);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithANullLockExpiryAreReturnedWithAllProperties()
    {
        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
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
        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
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
        var sut = new SqlMessageRepository<Tree>(_messageDbContext);
        var outboxMessageRow = BuildMessageSqlRow(null,
                    DateTime.UtcNow.AddSeconds(secondsTillRetryAfterElapses));
        await AddMessageToDatabaseAsync(outboxMessageRow);
        var messages = await sut.GetAsync();

        Assert.Equal(expectedMessageCount, messages.Count());
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

    private static void AssertPropertiesReturned(MessageSqlRow outboxMessageRow, Message<Tree>? outboxMessage)
    {
        if (outboxMessage is not null)
        {
            Assert.Equal(outboxMessageRow.MessageBlob, JsonSerializer.Serialize(outboxMessage.MessageObject));
            Assert.Equal(outboxMessageRow.CorrelationId, outboxMessage.CorrelationId);
            Assert.Equal(outboxMessageRow.AttemptCount, outboxMessage.AttemptCount);
            Assert.Equal(outboxMessageRow.LastAttempt, outboxMessage.LastAttempt);
            Assert.Equal(outboxMessageRow.LockExpiry, outboxMessage.LockExpiry);
            Assert.Equal(outboxMessageRow.RetryAfter, outboxMessage.RetryAfter);
        }
    }
}
