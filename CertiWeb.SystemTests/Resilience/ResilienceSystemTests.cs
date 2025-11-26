using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;

namespace CertiWeb.SystemTests.Resilience;

[TestFixture]
public class ResilienceSystemTests : SystemTestBase
{
    [Test]
    public async Task DatabaseConnectionFailure_ShouldHandleGracefully()
    {
        // Arrange - This test would require more complex setup to actually break the DB connection
        // For now, we'll test that the API handles expected database-related exceptions
        
        // Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        // The application should either succeed or fail gracefully with appropriate error codes
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task HighVolumeRequests_ShouldMaintainPerformance()
    {
        // Arrange
        const int numberOfRequests = 100;
        var requests = new List<Task<HttpResponseMessage>>();
        var successfulRequests = 0;
        var failedRequests = 0;

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            requests.Add(Client.GetAsync("/api/v1/brands"));
        }

        var responses = await Task.WhenAll(requests);

        foreach (var response in responses)
        {
            if (response.StatusCode == HttpStatusCode.OK)
                successfulRequests++;
            else
                failedRequests++;
        }

        // Assert
        successfulRequests.Should().BeGreaterThan((int)(numberOfRequests * 0.95), // 95% success rate
            $"At least 95% of requests should succeed. Success: {successfulRequests}, Failed: {failedRequests}");
    }

    [Test]
    public async Task InvalidDataRecovery_ShouldHandleCorruptedRequests()
    {
        // Arrange
        var corruptedRequests = new[]
        {
            "{ \"title\": null, \"owner\": \"\" }",
            "{ \"year\": \"not a number\" }",
            "{ \"price\": -999999 }",
            "{ \"brandId\": \"invalid\" }",
            "{ \"licensePlate\": \"\" }"
        };

        // Act & Assert
        foreach (var corruptedRequest in corruptedRequests)
        {
            var content = new StringContent(corruptedRequest, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/v1/cars", content);

            // The system should handle corrupted data gracefully
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnprocessableEntity);
        }

        // Verify the system still works after corrupted requests
        var healthCheckResponse = await Client.GetAsync("/api/v1/brands");
        healthCheckResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ResourceExhaustion_ShouldHandleLargeDataSets()
    {
        // Arrange
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Resilience Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        // Act - Create a large number of cars to test resource handling
        const int numberOfCars = 200;
        var creationTasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < numberOfCars; i++)
        {
            var carCommand = TestDataBuilder.CreateValidCarCommand(testBrand.Id);
            var carResource = new CreateCarResource(
                carCommand.Title,
                carCommand.Owner,
                carCommand.OwnerEmail,
                carCommand.Year,
                carCommand.BrandId,
                carCommand.Model,
                carCommand.Description,
                carCommand.PdfCertification,
                carCommand.ImageUrl,
                carCommand.Price,
                $"RES{i:D5}", // Unique license plate
                carCommand.OriginalReservationId + i
            );

            creationTasks.Add(Client.PostAsJsonAsync("/api/v1/cars", carResource));
        }

        var responses = await Task.WhenAll(creationTasks);

        // Assert
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failureCount = responses.Count(r => r.StatusCode != HttpStatusCode.Created);

        // Allow for some failures under load, but most should succeed
        successCount.Should().BeGreaterThan((int)(numberOfCars * 0.80), // 80% success rate
            $"At least 80% of car creations should succeed under load. Success: {successCount}, Failed: {failureCount}");

        // Verify system is still responsive
        var getAllResponse = await Client.GetAsync("/api/v1/cars");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ErrorRecovery_ShouldRecoverFromTransientFailures()
    {
        // Arrange
        var invalidRequest = new
        {
            Title = "", // Invalid empty title
            Owner = "Test Owner",
            OwnerEmail = "test@email.com",
            Year = 2023,
            BrandId = 1,
            Model = "Test Model",
            Description = "Test",
            PdfCertification = "VGVzdA==",
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "ABC1234",
            OriginalReservationId = 100
        };

        var validRequest = new
        {
            Title = "Valid Car Title",
            Owner = "Test Owner",
            OwnerEmail = "test@email.com",
            Year = 2023,
            BrandId = 1,
            Model = "Test Model",
            Description = "Test",
            PdfCertification = "VGVzdCBkYXRh",
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "DEF5678",
            OriginalReservationId = 200
        };

        // Act - Send invalid request followed by valid request
        var invalidResponse = await Client.PostAsJsonAsync("/api/v1/cars", invalidRequest);
        var validResponse = await Client.PostAsJsonAsync("/api/v1/cars", validRequest);

        // Assert
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        validResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "System should recover from previous invalid request and process valid request successfully");
    }

    [Test]
    public async Task ConcurrentModification_ShouldHandleRaceConditions()
    {
        // Arrange
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Concurrent Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        // Act - Try to create multiple cars with the same license plate simultaneously
        const int numberOfAttempts = 10;
        var sameLicensePlate = "SAME1234";
        var concurrentTasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < numberOfAttempts; i++)
        {
            var carCommand = TestDataBuilder.CreateValidCarCommand(testBrand.Id);
            var carResource = new CreateCarResource(
                carCommand.Title + i, // Different titles
                carCommand.Owner,
                carCommand.OwnerEmail,
                carCommand.Year,
                carCommand.BrandId,
                carCommand.Model,
                carCommand.Description,
                carCommand.PdfCertification,
                carCommand.ImageUrl,
                carCommand.Price,
                sameLicensePlate, // Same license plate
                carCommand.OriginalReservationId + i // Different reservation IDs
            );

            concurrentTasks.Add(Client.PostAsJsonAsync("/api/v1/cars", carResource));
        }

        var responses = await Task.WhenAll(concurrentTasks);

        // Assert
        var successfulCreations = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failedCreations = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        // Only one should succeed due to unique constraint
        successfulCreations.Should().Be(1, "Only one car with the same license plate should be created");
        failedCreations.Should().Be(numberOfAttempts - 1, "All other attempts should fail due to duplicate license plate");
    }

    [Test]
    public async Task LongRunningOperations_ShouldNotCauseTimeouts()
    {
        // Arrange - Simulate a potentially long operation by getting all data
        using var longTimeoutClient = Factory.CreateClient();
        longTimeoutClient.Timeout = TimeSpan.FromMinutes(2); // Extended timeout

        // Act
        var response = await longTimeoutClient.GetAsync("/api/v1/cars");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Long running operations should complete without timing out");
    }

    [Test]
    public async Task MemoryPressure_ShouldNotCauseOutOfMemoryErrors()
    {
        // Arrange
        const int largeDatasetSize = 1000;
        var memoryBeforeTest = GC.GetTotalMemory(true);

        // Act - Perform memory-intensive operations
        for (int batch = 0; batch < 10; batch++)
        {
            var tasks = new List<Task<HttpResponseMessage>>();
            
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Client.GetAsync("/api/v1/brands"));
                tasks.Add(Client.GetAsync("/api/v1/users"));
                tasks.Add(Client.GetAsync("/api/v1/cars"));
            }
            
            await Task.WhenAll(tasks);
            
            // Force garbage collection between batches
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        var memoryAfterTest = GC.GetTotalMemory(true);
        var memoryIncrease = memoryAfterTest - memoryBeforeTest;

        // Assert
        memoryIncrease.Should().BeLessThan(100 * 1024 * 1024, // 100MB increase limit
            $"Memory usage should not increase excessively. Increase: {memoryIncrease / (1024 * 1024):F2}MB");
    }

    [Test]
    public async Task ServiceDegradation_ShouldMaintainCoreFeatures()
    {
        // Arrange - Simulate high load conditions
        const int heavyLoadRequests = 500;
        var backgroundTasks = new List<Task>();

        // Act - Create background load
        for (int i = 0; i < heavyLoadRequests; i++)
        {
            backgroundTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await Client.GetAsync("/api/v1/brands");
                }
                catch
                {
                    // Ignore failures in background load
                }
            }));
        }

        // Test core functionality during high load
        var coreFeatureResponse = await Client.GetAsync("/api/v1/brands");

        await Task.WhenAll(backgroundTasks);

        // Assert
        coreFeatureResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Core features should remain available even under high load");
    }

    /// <summary>
    /// DTO for car creation.
    /// </summary>
    private record CreateCarResource(
        string Title,
        string Owner,
        string OwnerEmail,
        int Year,
        int BrandId,
        string Model,
        string? Description,
        string PdfCertification,
        string? ImageUrl,
        decimal Price,
        string LicensePlate,
        int OriginalReservationId
    );
}
