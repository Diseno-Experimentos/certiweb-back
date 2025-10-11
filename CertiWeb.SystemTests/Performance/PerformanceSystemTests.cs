using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;
using System.Diagnostics;

namespace CertiWeb.SystemTests.Performance;

[TestFixture]
public class PerformanceSystemTests : SystemTestBase
{
    [Test]
    public async Task GetAllBrands_PerformanceTest_ShouldCompleteWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = new Stopwatch();
        const int maxAcceptableTimeMs = 2000; // 2 seconds

        // Act
        stopwatch.Start();
        var response = await Client.GetAsync("/api/v1/brands");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxAcceptableTimeMs,
            $"API should respond within {maxAcceptableTimeMs}ms but took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task GetAllCars_WithLargeDataset_ShouldHandleLoad()
    {
        // Arrange - Create test brand
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Performance Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        // Create multiple cars for load testing
        const int numberOfCars = 50;
        var stopwatch = new Stopwatch();

        // Act - Create cars
        stopwatch.Start();
        var carCreationTasks = new List<Task<HttpResponseMessage>>();
        
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
                $"CAR{i:D4}01", // Unique license plate
                carCommand.OriginalReservationId + i // Unique reservation ID
            );
            
            carCreationTasks.Add(Client.PostAsJsonAsync("/api/v1/cars", carResource));
        }

        await Task.WhenAll(carCreationTasks);
        
        // Act - Retrieve all cars
        var getAllResponse = await Client.GetAsync("/api/v1/cars");
        stopwatch.Stop();

        // Assert
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cars = await DeserializeResponseAsync<CarResource[]>(getAllResponse);
        cars!.Length.Should().BeGreaterOrEqualTo(numberOfCars);
        
        // Performance assertion
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, // 5 seconds for full operation
            $"Creating {numberOfCars} cars and retrieving all should complete within 5 seconds");
    }

    [Test]
    public async Task ConcurrentApiCalls_ShouldHandleMultipleSimultaneousRequests()
    {
        // Arrange
        const int numberOfConcurrentRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < numberOfConcurrentRequests; i++)
        {
            tasks.Add(Client.GetAsync("/api/v1/brands"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(numberOfConcurrentRequests);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        
        // Performance assertion
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)numberOfConcurrentRequests;
        averageResponseTime.Should().BeLessThan(500, // 500ms average per request
            $"Average response time should be under 500ms but was {averageResponseTime:F2}ms");
    }

    [Test]
    public async Task DatabaseOperations_BulkInsert_ShouldHandleLargeDataVolumes()
    {
        // Arrange
        const int numberOfUsers = 100;
        var users = new List<CertiWeb.API.Users.Domain.Model.Aggregates.User>();
        var userCommands = TestDataBuilder.CreateMultipleUserCommands(numberOfUsers);

        foreach (var command in userCommands)
        {
            users.Add(new CertiWeb.API.Users.Domain.Model.Aggregates.User(command));
        }

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        using (var context = GetFreshDbContext())
        {
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, // 3 seconds for bulk insert
            $"Bulk insert of {numberOfUsers} users should complete within 3 seconds");

        // Verify data was inserted
        var response = await Client.GetAsync("/api/v1/users");
        var retrievedUsers = await DeserializeResponseAsync<UserResource[]>(response);
        retrievedUsers!.Length.Should().Be(numberOfUsers);
    }

    [Test]
    public async Task ApiResponseTimes_ShouldBeConsistentAcrossMultipleCalls()
    {
        // Arrange
        const int numberOfCalls = 10;
        var responseTimes = new List<long>();

        // Act
        for (int i = 0; i < numberOfCalls; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await Client.GetAsync("/api/v1/brands");
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
            
            // Small delay between requests
            await Task.Delay(100);
        }

        // Assert
        var averageTime = responseTimes.Average();
        var maxTime = responseTimes.Max();
        var minTime = responseTimes.Min();
        var standardDeviation = CalculateStandardDeviation(responseTimes);

        averageTime.Should().BeLessThan(1000, $"Average response time should be under 1 second but was {averageTime:F2}ms");
        maxTime.Should().BeLessThan(2000, $"Maximum response time should be under 2 seconds but was {maxTime}ms");
        standardDeviation.Should().BeLessThan(500, $"Response times should be consistent (std dev < 500ms) but was {standardDeviation:F2}ms");
    }

    [Test]
    public async Task MemoryUsage_LargeApiOperations_ShouldNotCauseMemoryLeaks()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        const int numberOfOperations = 50;

        // Act - Perform multiple operations
        for (int i = 0; i < numberOfOperations; i++)
        {
            await Client.GetAsync("/api/v1/brands");
            await Client.GetAsync("/api/v1/users");
            await Client.GetAsync("/api/v1/cars");
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert - Memory increase should be reasonable
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024, // 50MB
            $"Memory increase should be reasonable but increased by {memoryIncrease / (1024 * 1024):F2}MB");
    }

    /// <summary>
    /// Helper method to calculate standard deviation.
    /// </summary>
    private static double CalculateStandardDeviation(IEnumerable<long> values)
    {
        var avg = values.Average();
        var sumOfSquaresOfDifferences = values.Select(val => (val - avg) * (val - avg)).Sum();
        return Math.Sqrt(sumOfSquaresOfDifferences / values.Count());
    }

    /// <summary>
    /// DTO classes for response deserialization.
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

    private record CarResource(
        int Id,
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

    private record UserResource(int Id, string Name, string Email, string Plan);
}
