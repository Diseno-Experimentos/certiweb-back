using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;

namespace CertiWeb.SystemTests.Certifications.REST;

[TestFixture]
public class BrandsControllerSystemTests : SystemTestBase
{
    [Test]
    public async Task GetAllBrands_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var expectedPath = "/api/v1/brands";

        // Act
        var response = await Client.GetAsync(expectedPath);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetAllBrands_ShouldReturnValidJsonStructure()
    {
        // Arrange
        var expectedPath = "/api/v1/brands";

        // Act
        var response = await Client.GetAsync(expectedPath);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON array
        var brands = JsonSerializer.Deserialize<JsonElement[]>(content);
        brands.Should().NotBeNull();
    }

    [Test]
    public async Task GetAllBrands_WithSeededData_ShouldReturnExpectedBrands()
    {
        // Arrange
        var testBrands = new List<Brand>
        {
            new("Toyota"),
            new("Honda"),
            new("Nissan")
        };

        // Seed test data
        using (var context = GetFreshDbContext())
        {
            context.Brands.AddRange(testBrands);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/v1/brands");
        var brands = await DeserializeResponseAsync<BrandDto[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        brands.Should().NotBeNull();
        brands!.Length.Should().BeGreaterOrEqualTo(3);
        brands.Select(b => b.Name).Should().Contain(new[] { "Toyota", "Honda", "Nissan" });
    }

    [Test]
    public async Task GetAllBrands_ShouldReturnOnlyActiveBrands()
    {
        // Arrange
        var activeBrand = new Brand("Active Brand");
        var inactiveBrand = new Brand("Inactive Brand") { IsActive = false };

        using (var context = GetFreshDbContext())
        {
            context.Brands.AddRange(activeBrand, inactiveBrand);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/v1/brands");
        var brands = await DeserializeResponseAsync<BrandDto[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        brands.Should().NotBeNull();
        brands!.Should().NotContain(b => b.Name == "Inactive Brand");
        brands.Should().Contain(b => b.Name == "Active Brand");
    }

    [Test]
    public async Task GetAllBrands_WithEmptyDatabase_ShouldReturnEmptyArray()
    {
        // Arrange - Clear database manually as SetUp seeds it
        using (var context = GetFreshDbContext())
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM brands;");
        }
        
        // Act
        var response = await Client.GetAsync("/api/v1/brands");
        var brands = await DeserializeResponseAsync<BrandDto[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        brands.Should().NotBeNull();
        brands!.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllBrands_ShouldHaveCorrectResponseHeaders()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Should().ContainKey("Date");
    }

    [Test]
    public async Task GetAllBrands_ConcurrentRequests_ShouldHandleMultipleClients()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int numberOfConcurrentRequests = 5;

        // Act
        for (int i = 0; i < numberOfConcurrentRequests; i++)
        {
            tasks.Add(Client.GetAsync("/api/v1/brands"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(numberOfConcurrentRequests);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    /// <summary>
    /// DTO for brand response deserialization.
    /// </summary>
    private record BrandDto(int Id, string Name);
}
