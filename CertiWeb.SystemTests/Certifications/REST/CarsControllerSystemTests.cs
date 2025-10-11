using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;

namespace CertiWeb.SystemTests.Certifications.REST;

[TestFixture]
public class CarsControllerSystemTests : SystemTestBase
{
    private Brand _testBrand = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        
        // Arrange - Create test brand for car tests
        _testBrand = new Brand("Toyota");
        using var context = GetFreshDbContext();
        context.Brands.Add(_testBrand);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task CreateCar_WithValidData_ShouldReturnCreatedStatusAndPersistToDatabase()
    {
        // Arrange
        var createCarResource = new CreateCarResource(
            Title: "Toyota Corolla 2023",
            Owner: "Juan Perez",
            OwnerEmail: "juan.perez@email.com",
            Year: 2023,
            BrandId: _testBrand.Id,
            Model: "Corolla",
            Description: "Excellent condition car",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/car-image.jpg",
            Price: 25000.00m,
            LicensePlate: TestDataBuilder.GenerateValidLicensePlate(),
            OriginalReservationId: 100
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", createCarResource);
        var createdCar = await DeserializeResponseAsync<CarResource>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        createdCar.Should().NotBeNull();
        createdCar!.Title.Should().Be(createCarResource.Title);
        createdCar.Owner.Should().Be(createCarResource.Owner);
        createdCar.Id.Should().BeGreaterThan(0);

        // Verify persistence in database
        using var context = GetFreshDbContext();
        var savedCar = await context.Cars.FindAsync(createdCar.Id);
        savedCar.Should().NotBeNull();
        savedCar!.Title.Should().Be(createCarResource.Title);
    }

    [Test]
    public async Task CreateCar_WithInvalidYear_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidCarResource = new CreateCarResource(
            Title: "Invalid Car",
            Owner: "Test Owner",
            OwnerEmail: "test@email.com",
            Year: 1800, // Invalid year
            BrandId: _testBrand.Id,
            Model: "Test Model",
            Description: "Test Description",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/image.jpg",
            Price: 25000.00m,
            LicensePlate: TestDataBuilder.GenerateValidLicensePlate(),
            OriginalReservationId: 200
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", invalidCarResource);
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Validation error");
    }

    [Test]
    public async Task CreateCar_WithDuplicateLicensePlate_ShouldReturnBadRequest()
    {
        // Arrange
        var licensePlate = TestDataBuilder.GenerateValidLicensePlate();
        
        var firstCarResource = new CreateCarResource(
            Title: "First Car",
            Owner: "Owner One",
            OwnerEmail: "owner1@email.com",
            Year: 2023,
            BrandId: _testBrand.Id,
            Model: "Model One",
            Description: "Description One",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/image1.jpg",
            Price: 25000.00m,
            LicensePlate: licensePlate,
            OriginalReservationId: 300
        );

        var duplicateCarResource = new CreateCarResource(
            Title: "Second Car",
            Owner: "Owner Two",
            OwnerEmail: "owner2@email.com",
            Year: 2024,
            BrandId: _testBrand.Id,
            Model: "Model Two",
            Description: "Description Two",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/image2.jpg",
            Price: 30000.00m,
            LicensePlate: licensePlate, // Same license plate
            OriginalReservationId: 400
        );

        // Act
        var firstResponse = await Client.PostAsJsonAsync("/api/v1/cars", firstCarResource);
        var secondResponse = await Client.PostAsJsonAsync("/api/v1/cars", duplicateCarResource);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAllCars_ShouldReturnSuccessStatusCode()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/cars");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetAllCars_WithExistingCars_ShouldReturnAllCars()
    {
        // Arrange - Create test cars
        var carCommands = TestDataBuilder.CreateMultipleCarCommands(3, _testBrand.Id);
        
        foreach (var command in carCommands)
        {
            var resource = CreateCarResourceFromCommand(command);
            await Client.PostAsJsonAsync("/api/v1/cars", resource);
        }

        // Act
        var response = await Client.GetAsync("/api/v1/cars");
        var cars = await DeserializeResponseAsync<CarResource[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        cars.Should().NotBeNull();
        cars!.Length.Should().BeGreaterOrEqualTo(3);
    }

    [Test]
    public async Task GetCarById_WithExistingCar_ShouldReturnCar()
    {
        // Arrange
        var createCarResource = new CreateCarResource(
            Title: "Test Car",
            Owner: "Test Owner",
            OwnerEmail: "test@email.com",
            Year: 2023,
            BrandId: _testBrand.Id,
            Model: "Test Model",
            Description: "Test Description",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/image.jpg",
            Price: 25000.00m,
            LicensePlate: TestDataBuilder.GenerateValidLicensePlate(),
            OriginalReservationId: 500
        );

        var createResponse = await Client.PostAsJsonAsync("/api/v1/cars", createCarResource);
        var createdCar = await DeserializeResponseAsync<CarResource>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/v1/cars/{createdCar!.Id}");
        var car = await DeserializeResponseAsync<CarResource>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        car.Should().NotBeNull();
        car!.Id.Should().Be(createdCar.Id);
        car.Title.Should().Be(createCarResource.Title);
    }

    [Test]
    public async Task GetCarById_WithNonExistingCar_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = 99999;

        // Act
        var response = await Client.GetAsync($"/api/v1/cars/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateCar_EndToEndFlow_ShouldCompleteSuccessfully()
    {
        // Arrange - Full end-to-end test
        var carResource = new CreateCarResource(
            Title: "End-to-End Test Car",
            Owner: "E2E Test Owner",
            OwnerEmail: "e2e@test.com",
            Year: 2023,
            BrandId: _testBrand.Id,
            Model: "E2E Model",
            Description: "End-to-end test description",
            PdfCertification: TestDataBuilder.GenerateValidBase64(),
            ImageUrl: "https://example.com/e2e-image.jpg",
            Price: 35000.00m,
            LicensePlate: TestDataBuilder.GenerateValidLicensePlate(),
            OriginalReservationId: 600
        );

        // Act - Create car
        var createResponse = await Client.PostAsJsonAsync("/api/v1/cars", carResource);
        var createdCar = await DeserializeResponseAsync<CarResource>(createResponse);

        // Act - Verify it appears in GetAll
        var getAllResponse = await Client.GetAsync("/api/v1/cars");
        var allCars = await DeserializeResponseAsync<CarResource[]>(getAllResponse);

        // Act - Verify it can be retrieved by ID
        var getByIdResponse = await Client.GetAsync($"/api/v1/cars/{createdCar!.Id}");
        var retrievedCar = await DeserializeResponseAsync<CarResource>(getByIdResponse);

        // Assert - All operations should succeed
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        allCars.Should().Contain(c => c.Id == createdCar.Id);
        retrievedCar!.Title.Should().Be(carResource.Title);
        
        // Assert - Verify in database
        using var context = GetFreshDbContext();
        var dbCar = await context.Cars.FindAsync(createdCar.Id);
        dbCar.Should().NotBeNull();
        dbCar!.Title.Should().Be(carResource.Title);
    }

    /// <summary>
    /// Helper method to convert command to resource.
    /// </summary>
    private static CreateCarResource CreateCarResourceFromCommand(CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand command)
    {
        return new CreateCarResource(
            command.Title,
            command.Owner,
            command.OwnerEmail,
            command.Year,
            command.BrandId,
            command.Model,
            command.Description,
            command.PdfCertification,
            command.ImageUrl,
            command.Price,
            command.LicensePlate,
            command.OriginalReservationId
        );
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

    private record ErrorResponse(string Message, string? Details);
}
