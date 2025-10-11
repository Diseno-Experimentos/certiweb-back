using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.API.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace CertiWeb.SystemTests.Infrastructure;

/// <summary>
/// Base class for system tests that provides HTTP client and database setup.
/// </summary>
public abstract class SystemTestBase : IDisposable
{
    protected CertiWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        // Arrange - Create application factory and HTTP client
        Factory = new CertiWebApplicationFactory();
        Client = Factory.CreateClient();
        
        // Setup database
        DbContext = await Factory.CreateAndSeedDatabaseAsync();
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDown()
    {
        // Cleanup
        await DbContext.Database.EnsureDeletedAsync();
        DbContext?.Dispose();
        Client?.Dispose();
        await Factory?.DisposeAsync()!;
    }

    [SetUp]
    public virtual async Task SetUp()
    {
        // Override in derived classes if needed
        await Task.CompletedTask;
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Override in derived classes if needed
        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to create JSON content for HTTP requests.
    /// </summary>
    protected static JsonContent CreateJsonContent<T>(T data)
    {
        return JsonContent.Create(data);
    }

    /// <summary>
    /// Helper method to deserialize HTTP response content.
    /// </summary>
    protected static async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var jsonString = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Gets a fresh DbContext for database assertions.
    /// </summary>
    protected AppDbContext GetFreshDbContext()
    {
        return Factory.GetDbContext();
    }

    public void Dispose()
    {
        // Cleanup will be handled in OneTimeTearDown
        GC.SuppressFinalize(this);
    }
}
