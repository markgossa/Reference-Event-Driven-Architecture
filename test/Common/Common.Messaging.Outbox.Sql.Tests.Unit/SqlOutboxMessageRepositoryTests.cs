using Common.Messaging.Outbox.Models;
using Common.Messaging.Outbox.Sql.Models;
using Common.Messaging.Outbox.Sql.Tests.Unit.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Text.Json;
using Xunit;

namespace Common.Messaging.Outbox.Sql.Tests.Unit;
public class SqlOutboxMessageRepositoryTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly OutboxMessageDbContext _outboxMessageDbContext;

    public SqlOutboxMessageRepositoryTests()
    {
        var connection = CreateInMemoryDatabaseConnection();
        BuildInMemoryDatabase(connection);

        _serviceProvider = new ServiceCollection()
            .AddDbContext<OutboxMessageDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();
        _outboxMessageDbContext = _serviceProvider.GetRequiredService<OutboxMessageDbContext>();
    }

    [Fact]
    public async Task GivenNewInstance_WhenAMessageIsAdded_ThenMessageIsSavedInSqlAndMessageObjectSerializedToJson()
    {
        var messageToAdd = new OutboxMessage<Tree>(Guid.NewGuid().ToString(), new Tree("Sycamore", 5))
        {
            AttemptCount = 1,
            LastAttempt = DateTime.UtcNow.AddDays(-1),
            LockExpiry = DateTime.UtcNow.AddDays(-1)
        };

        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        await sut.AddAsync(messageToAdd);

        AssertMessagePropertiesSavedToDatabase(messageToAdd);
    }

#nullable disable
    [Theory]
    [InlineData("15/07/2022 15:03", "15/07/2022 15:02", 2, "16/07/2022 15:30")]
    [InlineData(null, null, 5, null)]
    public async Task GivenNewInstance_WhenAMessageIsUpdated_ThenMessageIsUpdatedInSql(string lockExpiry,
        string lastAttempt, int attemptCount, string retryAfter)
    {
        var message = BuildOutboxMessageSqlRow();
        await AddMessageToDatabaseAsync(message);

        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        var messageToUpdate = new OutboxMessage<Tree>(message.CorrelationId, JsonSerializer.Deserialize<Tree>(message.MessageBlob))
        {
            LockExpiry = ParseDate(lockExpiry),
            LastAttempt = ParseDate(lastAttempt),
            AttemptCount = attemptCount,
            RetryAfter = ParseDate(retryAfter)
        };

        await sut.UpdateAsync(new List<OutboxMessage<Tree>> { messageToUpdate });

        AssertMessagePropertiesSavedToDatabase(messageToUpdate);
    }
#nullable enable

    [Fact]
    public async Task GivenNewInstance_WhenAMessageIsRemoved_ThenMessageIsRemovedFromSql()
    {
        var message = BuildOutboxMessageSqlRow();

        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        await AddMessageToDatabaseAsync(message);
        await sut.RemoveAsync(new List<string> { message.CorrelationId });

        Assert.Empty(_outboxMessageDbContext.Messages);
    }

    [Fact]
    public async Task GivenNewInstance_WhenIGetMessages_ThenMessagesWithANullLockExpiryAreReturnedWithAllProperties()
    {
        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        var outboxMessageRow = BuildOutboxMessageSqlRow();
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
        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        var outboxMessageRow = BuildOutboxMessageSqlRow(DateTime.UtcNow.AddSeconds(secondsTillLockExpires));
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
        var sut = new SqlOutboxMessageRepository<Tree>(_outboxMessageDbContext);
        var outboxMessageRow = BuildOutboxMessageSqlRow(null,
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
        var options = new DbContextOptionsBuilder<OutboxMessageDbContext>()
            .UseSqlite(connection).Options;
        ResetSqliteDatabase(new OutboxMessageDbContext(options));
    }

    private static void ResetSqliteDatabase(OutboxMessageDbContext dbContext)
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    private void AssertMessagePropertiesSavedToDatabase(OutboxMessage<Tree> messageToAdd)
    {
        var savedMessages = _outboxMessageDbContext.Messages;
        Assert.Single(savedMessages);
        Assert.Single(savedMessages.Where(m => m.CorrelationId == messageToAdd.CorrelationId));
        Assert.Single(savedMessages.Where(m => m.LastAttempt == messageToAdd.LastAttempt));
        Assert.Single(savedMessages.Where(m => m.AttemptCount == messageToAdd.AttemptCount));
        Assert.Single(savedMessages.Where(m => m.LockExpiry == messageToAdd.LockExpiry));
        Assert.Single(savedMessages.Where(m => m.RetryAfter == messageToAdd.RetryAfter));
        Assert.Single(savedMessages.Where(m => m.MessageBlob == JsonSerializer.Serialize(messageToAdd.MessageObject,
            new JsonSerializerOptions())));
    }

    private async Task AddMessageToDatabaseAsync(OutboxMessageSqlRow message)
    {
        await _outboxMessageDbContext.Messages.AddAsync(message);
        await _outboxMessageDbContext.SaveChangesAsync();
    }

    private static OutboxMessageSqlRow BuildOutboxMessageSqlRow(DateTime? lockExpiry = null,
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

    private static void AssertPropertiesReturned(OutboxMessageSqlRow outboxMessageRow, OutboxMessage<Tree>? outboxMessage)
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
