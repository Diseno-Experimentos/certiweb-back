using CertiWeb.API;
using CertiWeb.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace CertiWeb.SystemTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for system tests that provides an isolated test environment.
/// </summary>
public class CertiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public CertiWebApplicationFactory()
    {
        _databaseName = $"SystemTestDb_{Guid.NewGuid()}";
    }

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

            // Add InMemory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

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
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await context.Database.EnsureCreatedAsync();
        
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
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
