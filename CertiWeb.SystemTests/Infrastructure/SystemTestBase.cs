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

        // Add default authorization header so middleware accepts requests in tests
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");
        
        // Setup database
        DbContext = await Factory.CreateAndSeedDatabaseAsync();
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDown()
    {
        // Cleanup
        if (DbContext != null)
        {
            try
            {
                await DbContext.Database.EnsureDeletedAsync();
            }
            catch
            {
                // swallow - database may already be disposed/closed by the factory
            }

            try
            {
                DbContext.Dispose();
            }
            catch
            {
                // swallow
            }
        }

        Client?.Dispose();

        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }

    [SetUp]
    public virtual async Task SetUp()
    {
        // Ensure database is clean before each test to provide test isolation.
        try
        {
            if (DbContext != null)
            {
                await DbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM cars;");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM brands;");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM admin_users;");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM reservations;");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM users;");
                await DbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

                // Re-seed model-level data (brands, admin user) so tests that expect application defaults
                // have a consistent starting point. Use raw SQL to avoid EF tracking conflicts.
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PRAGMA foreign_keys = OFF;");

                // Insert brands (IDs 1..13)
                sb.AppendLine("DELETE FROM brands;");
                var brands = CertiWeb.API.Certifications.Infrastructure.BrandSeeder.GetPredefinedBrands();
                foreach (var b in brands)
                {
                    // Use parameters to avoid SQL injection and respect values
                    sb.AppendLine($"INSERT INTO brands (id, name, is_active) VALUES ({b.Id}, '{b.Name.Replace("'", "''")}', {(b.IsActive ? 1 : 0)});");
                }

                // Insert admin user
                sb.AppendLine("DELETE FROM admin_users;");
                var admin = CertiWeb.API.IAM.Infrastructure.Persistence.EFC.Seeders.AdminUserSeeder.GetAdminUser();
                sb.AppendLine($"INSERT INTO admin_users (id, name, email, password) VALUES ({admin.Id}, '{admin.Name.Replace("'", "''")}', '{admin.Email.Replace("'", "''")}', '{admin.Password.Replace("'", "''")}');");

                sb.AppendLine("PRAGMA foreign_keys = ON;");

                await DbContext.Database.ExecuteSqlRawAsync(sb.ToString());
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to clean and reseed test database before test.", ex);
        }
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
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return default;
        }

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
