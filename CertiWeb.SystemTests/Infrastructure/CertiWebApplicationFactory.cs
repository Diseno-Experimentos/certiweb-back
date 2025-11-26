using CertiWeb.API;
using CertiWeb.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CertiWeb.SystemTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for system tests that provides an isolated test environment.
/// </summary>
public class CertiWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    private readonly List<IServiceScope> _scopes = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Use SQLite in-memory with shared cache. Each DbContext should open its own
            // connection to the same shared in-memory 'file' to avoid contention when EF
            // initializes per-connection resources concurrently in multi-threaded tests.
            // Keep a separate 'keep alive' connection open to retain the shared in-memory DB.
            var sharedConnectionString = "DataSource=file:memdb?mode=memory&cache=shared";
            _connection = new SqliteConnection(sharedConnectionString);
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                // Do not reuse the same SqliteConnection for all contexts. Instead, let
                // EF create per-scope connections that point to the same shared in-memory
                // file via connection string. This avoids concurrent initialization of
                // functions on a single connection which can lead to errors like
                // 'unable to delete/modify user-function due to active statements'.
                options.UseSqlite(sharedConnectionString);
            });

            // Replace authentication/token services with test doubles so system tests don't require real tokens
            var tokenDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CertiWeb.API.Users.Application.Internal.OutboundServices.ITokenService));
            if (tokenDescriptor != null)
            {
                services.Remove(tokenDescriptor);
            }

            services.AddScoped<CertiWeb.API.Users.Application.Internal.OutboundServices.ITokenService, TestTokenService>();
            // Replace IUserQueryService with a lightweight test double so middleware and
            // authorization checks receive a predictable user without depending on seeded data.
            // Do not replace IUserQueryService globally; instead register an optional test-only
            // provider that the middleware will use when present. This avoids interfering with
            // controllers and application services that expect the real IUserQueryService.
            services.AddScoped<CertiWeb.API.Users.Infrastructure.Pipeline.Middleware.ITestUserProvider, TestUserProvider>();
            // Note: do not replace the IUserQueryService here — keep the real implementation
            // so controllers read from the test database. Only token service is replaced.
            // Reduce logging noise during tests
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates and seeds the database with test data.
    /// </summary>
    public async Task<AppDbContext> CreateAndSeedDatabaseAsync()
    {
        // Create a scope and keep it alive for the lifetime of the factory so the resolved
        // DbContext instances are not disposed prematurely.
        var scope = Services.CreateScope();
        _scopes.Add(scope);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Remove any global seed data (HasData) so tests run with a clean database by default.
        // Some application model configuration uses HasData to pre-populate tables which
        // makes tests that expect an empty DB flaky. Clear seeded tables here and allow
        // individual tests to seed what they need.
        // Ensure any model-level seed data is removed so tests start with a clean DB.
        // Use raw SQL delete commands to avoid EF change-tracking / FK issues and
        // surface any errors during cleanup instead of silently swallowing them.
        // Disable foreign key checks temporarily to ensure deletes succeed in any order.
        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

            // Remove commonly seeded/related tables used by the application.
            await context.Database.ExecuteSqlRawAsync("DELETE FROM cars;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM brands;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM admin_users;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM reservations;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM users;");

            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }
        catch (Exception ex)
        {
            // Surface cleanup errors so they can be fixed instead of hiding test failures.
            throw new InvalidOperationException("Failed to clear seeded tables in test database.", ex);
        }

        // Seed with test data if needed
        await SeedTestDataAsync(context);

        return context;
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        // This will be populated with test data as needed
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a fresh DbContext for assertions.
    /// </summary>
    public AppDbContext GetDbContext()
    {
        // Create a disposable scope for callers and keep it until the factory is disposed.
        var scope = Services.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public override async ValueTask DisposeAsync()
    {
        // Dispose tracked scopes first
        foreach (var scope in _scopes)
        {
            try
            {
                scope.Dispose();
            }
            catch
            {
                // swallow - we're disposing during test shutdown
            }
        }
        _scopes.Clear();

        // Close SQLite connection
        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch
        {
        }

        await base.DisposeAsync();
    }
}
