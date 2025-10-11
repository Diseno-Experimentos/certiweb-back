using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;

namespace CertiWeb.SystemTests.BusinessFlows;

[TestFixture]
public class BusinessFlowSystemTests : SystemTestBase
{
    [Test]
    public async Task CompleteCarCertificationFlow_ShouldWorkEndToEnd()
    {
        // Arrange - Business Flow: Complete car certification process
        
        // Step 1: Verify brands are available
        var brandsResponse = await Client.GetAsync("/api/v1/brands");
        brandsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var brands = await DeserializeResponseAsync<BrandDto[]>(brandsResponse);
        brands.Should().NotBeNull().And.NotBeEmpty("Brands should be available for car registration");

        var selectedBrand = brands!.First();

        // Step 2: Create a car certification
        var carData = new CreateCarResource(
            Title: "Toyota Camry 2023 - Complete Flow Test",
            Owner: "María García López",
            OwnerEmail: "maria.garcia@certificaciones.com",
            Year: 2023,
            BrandId: selectedBrand.Id,
            Model: "Camry",
            Description: "Vehículo en excelente estado para certificación completa",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://certificaciones.com/images/toyota-camry-2023.jpg",
            Price: 45000.00m,
            LicensePlate: "BFC2023A",
            OriginalReservationId: 10001
        );

        // Act - Step 2: Submit car for certification
        var createResponse = await Client.PostAsJsonAsync("/api/v1/cars", carData);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCar = await DeserializeResponseAsync<CarResource>(createResponse);

        // Assert - Step 2: Verify car was created correctly
        createdCar.Should().NotBeNull();
        createdCar!.Title.Should().Be(carData.Title);
        createdCar.Owner.Should().Be(carData.Owner);
        createdCar.Id.Should().BeGreaterThan(0);

        // Act - Step 3: Retrieve the created car to verify it's accessible
        var getCarResponse = await Client.GetAsync($"/api/v1/cars/{createdCar.Id}");
        getCarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedCar = await DeserializeResponseAsync<CarResource>(getCarResponse);

        // Assert - Step 3: Verify retrieved car matches created car
        retrievedCar.Should().NotBeNull();
        retrievedCar!.Id.Should().Be(createdCar.Id);
        retrievedCar.LicensePlate.Should().Be(carData.LicensePlate.ToUpperInvariant());
        retrievedCar.Price.Should().Be(carData.Price);

        // Act - Step 4: Verify car appears in general listings
        var getAllCarsResponse = await Client.GetAsync("/api/v1/cars");
        getAllCarsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allCars = await DeserializeResponseAsync<CarResource[]>(getAllCarsResponse);

        // Assert - Step 4: Verify car is included in listings
        allCars.Should().NotBeNull();
        allCars!.Should().Contain(c => c.Id == createdCar.Id,
            "Newly created car should appear in general car listings");

        // Act - Step 5: Verify car appears in brand-specific listings
        var getBrandCarsResponse = await Client.GetAsync($"/api/v1/cars/brand/{selectedBrand.Id}");
        
        // Assert - Step 5: Verify car appears in brand-specific listings (if endpoint exists)
        if (getBrandCarsResponse.StatusCode == HttpStatusCode.OK)
        {
            var brandCars = await DeserializeResponseAsync<CarResource[]>(getBrandCarsResponse);
            brandCars.Should().NotBeNull();
            brandCars!.Should().Contain(c => c.Id == createdCar.Id,
                "Car should appear in brand-specific listings");
        }

        // Final verification: Database consistency
        using var context = GetFreshDbContext();
        var dbCar = await context.Cars.FindAsync(createdCar.Id);
        dbCar.Should().NotBeNull("Car should exist in database");
        dbCar!.Title.Should().Be(carData.Title);
        dbCar.Owner.Should().Be(carData.Owner);
    }

    [Test]
    public async Task UserSearchAndCarDiscoveryFlow_ShouldProvideComprehensiveResults()
    {
        // Arrange - Business Flow: User searching for cars
        
        // Step 1: Create test data - multiple brands and cars
        var toyotaBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Toyota Flow Test");
        var hondaBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Honda Flow Test");

        using (var context = GetFreshDbContext())
        {
            context.Brands.AddRange(toyotaBrand, hondaBrand);
            await context.SaveChangesAsync();
        }

        // Create multiple cars for testing search functionality
        var testCars = new[]
        {
            new CreateCarResource(
                Title: "Toyota Corolla 2023 - Económico",
                Owner: "Juan Pérez",
                OwnerEmail: "juan@email.com",
                Year: 2023,
                BrandId: toyotaBrand.Id,
                Model: "Corolla",
                Description: "Vehículo económico ideal para ciudad",
                PdfCertification: TestDataBuilder.GenerateValidBase64(),
                ImageUrl: "https://example.com/corolla.jpg",
                Price: 25000.00m,
                LicensePlate: "FLOW001A",
                OriginalReservationId: 20001
            ),
            new CreateCarResource(
                Title: "Honda Civic 2024 - Deportivo",
                Owner: "Ana Martín",
                OwnerEmail: "ana@email.com",
                Year: 2024,
                BrandId: hondaBrand.Id,
                Model: "Civic",
                Description: "Vehículo deportivo con excelente rendimiento",
                PdfCertification: TestDataBuilder.GenerateValidBase64(),
                ImageUrl: "https://example.com/civic.jpg",
                Price: 32000.00m,
                LicensePlate: "FLOW002B",
                OriginalReservationId: 20002
            ),
            new CreateCarResource(
                Title: "Toyota Camry 2023 - Lujo",
                Owner: "Carlos Ruiz",
                OwnerEmail: "carlos@email.com",
                Year: 2023,
                BrandId: toyotaBrand.Id,
                Model: "Camry",
                Description: "Vehículo de lujo con todas las comodidades",
                PdfCertification: TestDataBuilder.GenerateValidBase64(),
                ImageUrl: "https://example.com/camry.jpg",
                Price: 45000.00m,
                LicensePlate: "FLOW003C",
                OriginalReservationId: 20003
            )
        };

        // Create the test cars
        var createdCarIds = new List<int>();
        foreach (var carData in testCars)
        {
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCarIds.Add(createdCar!.Id);
        }

        // Act & Assert - Step 2: User browsing all available brands
        var brandsResponse = await Client.GetAsync("/api/v1/brands");
        brandsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var brands = await DeserializeResponseAsync<BrandDto[]>(brandsResponse);
        
        brands.Should().NotBeNull();
        brands!.Should().Contain(b => b.Name == "Toyota Flow Test");
        brands.Should().Contain(b => b.Name == "Honda Flow Test");

        // Act & Assert - Step 3: User viewing all cars
        var allCarsResponse = await Client.GetAsync("/api/v1/cars");
        allCarsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allCars = await DeserializeResponseAsync<CarResource[]>(allCarsResponse);
        
        allCars.Should().NotBeNull();
        allCars!.Length.Should().BeGreaterOrEqualTo(3);
        allCars.Should().Contain(c => c.Model == "Corolla");
        allCars.Should().Contain(c => c.Model == "Civic");
        allCars.Should().Contain(c => c.Model == "Camry");

        // Act & Assert - Step 4: User examining specific cars
        foreach (var carId in createdCarIds)
        {
            var carResponse = await Client.GetAsync($"/api/v1/cars/{carId}");
            carResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var car = await DeserializeResponseAsync<CarResource>(carResponse);
            
            car.Should().NotBeNull();
            car!.Id.Should().Be(carId);
            car.Title.Should().NotBeNullOrEmpty();
            car.Owner.Should().NotBeNullOrEmpty();
            car.Price.Should().BeGreaterThan(0);
        }

        // Step 5: Verify price range diversity (business logic test)
        var prices = allCars.Where(c => createdCarIds.Contains(c.Id)).Select(c => c.Price).ToArray();
        prices.Should().Contain(p => p < 30000, "Should have budget-friendly options");
        prices.Should().Contain(p => p > 40000, "Should have luxury options");
    }

    [Test]
    public async Task CarCertificationWithValidationFlow_ShouldHandleErrorsGracefully()
    {
        // Arrange - Business Flow: Car certification with validation errors and recovery
        
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Validation Flow Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        // Step 1: Attempt to create car with invalid data
        var invalidCarData = new CreateCarResource(
            Title: "", // Invalid: empty title
            Owner: "Test Owner",
            OwnerEmail: "invalid-email", // Invalid: bad email format
            Year: 1800, // Invalid: year too old
            BrandId: testBrand.Id,
            Model: "Test Model",
            Description: "Validation flow test",
            PdfCertification: "ABC", // Invalid: too short
            ImageUrl: "https://example.com/image.jpg",
            Price: -1000, // Invalid: negative price
            LicensePlate: "AB", // Invalid: too short
            OriginalReservationId: 30001
        );

        // Act - Step 1: Submit invalid data
        var invalidResponse = await Client.PostAsJsonAsync("/api/v1/cars", invalidCarData);

        // Assert - Step 1: Should receive validation error
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Invalid data should be rejected with proper error response");

        // Step 2: Correct the data and resubmit
        var correctedCarData = new CreateCarResource(
            Title: "Corrected Car Title",
            Owner: "Test Owner",
            OwnerEmail: "valid@email.com",
            Year: 2023,
            BrandId: testBrand.Id,
            Model: "Test Model",
            Description: "Corrected validation flow test",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/image.jpg",
            Price: 25000.00m,
            LicensePlate: "VALID123",
            OriginalReservationId: 30002
        );

        // Act - Step 2: Submit corrected data
        var correctedResponse = await Client.PostAsJsonAsync("/api/v1/cars", correctedCarData);

        // Assert - Step 2: Should succeed after correction
        correctedResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "Corrected data should be accepted and car should be created successfully");

        var createdCar = await DeserializeResponseAsync<CarResource>(correctedResponse);
        createdCar.Should().NotBeNull();
        createdCar!.Title.Should().Be(correctedCarData.Title);

        // Step 3: Verify the system remained stable after validation errors
        var healthCheckResponse = await Client.GetAsync("/api/v1/brands");
        healthCheckResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "System should remain stable after handling validation errors");
    }

    [Test]
    public async Task MultiUserCarRegistrationFlow_ShouldHandleConcurrentOperations()
    {
        // Arrange - Business Flow: Multiple users registering cars simultaneously
        
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Concurrent Flow Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        // Simulate multiple users creating cars simultaneously
        const int numberOfUsers = 10;
        var concurrentCarCreations = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < numberOfUsers; i++)
        {
            var carData = new CreateCarResource(
                Title: $"User {i + 1} Car",
                Owner: $"User Owner {i + 1}",
                OwnerEmail: $"user{i + 1}@email.com",
                Year: 2023,
                BrandId: testBrand.Id,
                Model: $"Model{i + 1}",
                Description: $"Car registered by user {i + 1}",
                PdfCertification: TestDataBuilder.GenerateValidBase64(),
                ImageUrl: $"https://example.com/car{i + 1}.jpg",
                Price: 25000.00m + (i * 1000), // Different prices
                LicensePlate: $"USR{i + 1:D3}23",
                OriginalReservationId: 40001 + i
            );

            concurrentCarCreations.Add(Client.PostAsJsonAsync("/api/v1/cars", carData));
        }

        // Act - Execute all car creations concurrently
        var responses = await Task.WhenAll(concurrentCarCreations);

        // Assert - All operations should succeed
        var successfulCreations = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        successfulCreations.Should().Be(numberOfUsers,
            $"All {numberOfUsers} concurrent car registrations should succeed");

        // Verify all cars were actually created and are retrievable
        var getAllResponse = await Client.GetAsync("/api/v1/cars");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allCars = await DeserializeResponseAsync<CarResource[]>(getAllResponse);
        
        var createdCars = allCars!.Where(c => c.Owner.Contains("User Owner")).ToArray();
        createdCars.Length.Should().Be(numberOfUsers,
            "All concurrently created cars should be retrievable");

        // Verify each car has unique data
        createdCars.Select(c => c.LicensePlate).Should().OnlyHaveUniqueItems(
            "Each car should have a unique license plate");
        createdCars.Select(c => c.OriginalReservationId).Should().OnlyHaveUniqueItems(
            "Each car should have a unique reservation ID");
    }

    /// <summary>
    /// DTO classes for business flow tests.
    /// </summary>
    private record BrandDto(int Id, string Name);
    
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
}
