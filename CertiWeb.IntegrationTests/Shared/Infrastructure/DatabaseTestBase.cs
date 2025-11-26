using CertiWeb.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CertiWeb.IntegrationTests.Shared.Infrastructure;

/// <summary>
/// Base class for integration tests that provides database context setup and cleanup.
/// </summary>
public abstract class DatabaseTestBase
{
    protected AppDbContext Context { get; private set; } = null!;
    protected DbContextOptions<AppDbContext> Options { get; private set; } = null!;
    private SqliteConnection? _connection;

    [SetUp]
    public virtual async Task SetUp()
    {
        // Arrange - Use SQLite in-memory provider with a dedicated connection per test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(Options);

        // Ensure database is created
        await Context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Cleanup - Dispose context and delete database
        if (Context != null)
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.DisposeAsync();
            Context = null!;
        }

        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
    }

    /// <summary>
    /// Creates a fresh context for testing scenarios that require multiple contexts.
    /// </summary>
    /// <returns>A new instance of AppDbContext</returns>
    protected AppDbContext CreateFreshContext()
    {
        return new AppDbContext(Options);
    }

    /// <summary>
    /// Saves changes and detaches all entities from the context.
    /// Useful for testing scenarios where you want to simulate fresh entity loading.
    /// </summary>
    protected async Task SaveChangesAndClearContext()
    {
        await Context.SaveChangesAsync();
        
        // Detach all entities
        var entries = Context.ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }
}
