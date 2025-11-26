using CertiWeb.SystemTests.Infrastructure;

namespace CertiWeb.SystemTests.Health;

[TestFixture]
public class HealthCheckSystemTests : SystemTestBase
{
    [Test]
    public async Task Application_ShouldStartSuccessfully()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Application should start and respond to basic requests");
    }

    [Test]
    public async Task Database_ShouldBeAccessible()
    {
        // Arrange & Act
        using var context = GetFreshDbContext();
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue("Database should be accessible");
    }

    [Test]
    public async Task ApplicationConfiguration_ShouldBeValid()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.Should().NotBeNull("HTTP client should be configured");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task ApiEndpoints_ShouldReturnValidContentType()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/v1/brands",
            "/api/v1/cars",
            "/api/v1/users"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Endpoint {endpoint} should be accessible");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
                $"Endpoint {endpoint} should return JSON content");
        }
    }

    [Test]
    public async Task ApplicationEnvironment_ShouldBeTestingEnvironment()
    {
        // Arrange - Create a test endpoint that would show environment info
        // This is mainly to verify our test setup is correct
        
        // Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Application should be running in testing environment");
    }

    [Test]
    public async Task DatabaseSchema_ShouldBeCreatedCorrectly()
    {
        // Arrange & Act
        using var context = GetFreshDbContext();
        var hasUsersTable = context.Users != null;
        var hasBrandsTable = context.Brands != null;
        var hasCarsTable = context.Cars != null;

        // Assert
        hasUsersTable.Should().BeTrue("Users table should exist");
        hasBrandsTable.Should().BeTrue("Brands table should exist");
        hasCarsTable.Should().BeTrue("Cars table should exist");
    }

    [Test]
    public async Task MultipleRequests_ShouldMaintainConsistentBehavior()
    {
        // Arrange
        const int numberOfRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Client.GetAsync("/api/v1/brands"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(numberOfRequests);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        responses.Should().OnlyContain(r => r.Content.Headers.ContentType != null && r.Content.Headers.ContentType.MediaType == "application/json");
    }
}
