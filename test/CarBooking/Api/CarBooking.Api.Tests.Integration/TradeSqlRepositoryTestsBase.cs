using LSE.Stocks.Infrastructure.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace LSE.Stocks.Api.Tests.Integration;

public class TradeSqlRepositoryTestsBase : IDisposable, IAsyncDisposable
{
    protected readonly ServiceProvider _serviceProvider;
    protected TradesDbContext? _tradesDbContext;

    public TradeSqlRepositoryTestsBase()
    {
        var connection = CreateInMemoryDatabaseConnection();
        BuildInMemoryDatabase(connection);

        _serviceProvider = new ServiceCollection()
            .AddDbContext<TradesDbContext>(o => o.UseSqlite(connection))
            .BuildServiceProvider();
        _tradesDbContext = _serviceProvider.GetRequiredService<TradesDbContext>();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    ~TradeSqlRepositoryTestsBase()
    {
        Dispose(disposing: false);
    }

    protected static DbConnection CreateInMemoryDatabaseConnection()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        return connection;
    }

    private static void BuildInMemoryDatabase(DbConnection connection)
    {
        var options = new DbContextOptionsBuilder<TradesDbContext>()
            .UseSqlite(connection).Options;
        ResetSqliteDatabase(new TradesDbContext(options));
    }

    protected static void ResetSqliteDatabase(TradesDbContext dbContext)
    {
        _ = dbContext.Database.EnsureDeleted();
        _ = dbContext.Database.EnsureCreated();
    }

    protected async Task AddRecordsToInMemoryDatabase<T>(IEnumerable<T> records) where T : class
    {
        if (_tradesDbContext is not null)
        {
            await SaveRecordsAsync(records);
        }
    }

    private async Task SaveRecordsAsync<T>(IEnumerable<T> records) where T : class
    {
        foreach (var record in records)
        {
            await _tradesDbContext!.AddAsync(record);
        }

        await _tradesDbContext!.SaveChangesAsync();
        _tradesDbContext.ChangeTracker.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tradesDbContext?.Dispose();
        }
    }

    private async Task DisposeAsyncCore()
    {
        if (_tradesDbContext is not null)
        {
            await _tradesDbContext.DisposeAsync();
        }

        _tradesDbContext = null;
    }
}