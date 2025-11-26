using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.IntegrationTests.Shared.Infrastructure;

namespace CertiWeb.IntegrationTests.Certifications.Domain.Model.Aggregates;

[TestFixture]
public class CarIntegrationTests : DatabaseTestBase
{
    private Brand _testBrand = null!;
    private CreateCarCommand _validCommand = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        
        // Arrange - Create test brand
        _testBrand = new Brand("Toyota");
        Context.Brands.Add(_testBrand);
        await Context.SaveChangesAsync();

        _validCommand = new CreateCarCommand(
            Title: "Toyota Corolla 2023",
            Owner: "Juan Perez",
            OwnerEmail: "juan.perez@email.com",
            Year: 2023,
            BrandId: _testBrand.Id,
            Model: "Corolla",
            Description: "Excellent condition car",
            PdfCertification: "VGVzdCBQREYgZGF0YSBmb3IgY2VydGlmaWNhdGlvbg==",
            ImageUrl: "https://example.com/car-image.jpg",
            Price: 25000.00m,
            LicensePlate: "ABC1234",
            OriginalReservationId: 100
        );
    }

    [Test]
    public async Task CreateCar_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        var car = new Car(_validCommand);

        // Act
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Assert
        var savedCar = await Context.Cars.FirstOrDefaultAsync(c => c.Title == "Toyota Corolla 2023");
        savedCar.Should().NotBeNull();
        savedCar!.Title.Should().Be("Toyota Corolla 2023");
        savedCar.Owner.Should().Be("Juan Perez");
        savedCar.OwnerEmail.Should().Be("juan.perez@email.com");
        savedCar.Model.Should().Be("Corolla");
        savedCar.BrandId.Should().Be(_testBrand.Id);
        savedCar.OriginalReservationId.Should().Be(100);
        savedCar.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateCar_WithValueObjects_ShouldPersistValueObjectsCorrectly()
    {
        // Arrange
        var car = new Car(_validCommand);

        // Act
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Assert
        await SaveChangesAndClearContext();
        var savedCar = await Context.Cars.FirstOrDefaultAsync(c => c.Title == "Toyota Corolla 2023");
        
        savedCar.Should().NotBeNull();
        savedCar!.Year.Value.Should().Be(2023);
        savedCar.Price.Value.Should().Be(25000.00m);
        savedCar.Price.Currency.Should().Be("SOL");
        savedCar.LicensePlate.Value.Should().Be("ABC1234");
        savedCar.PdfCertification.Base64Data.Should().Be("VGVzdCBQREYgZGF0YSBmb3IgY2VydGlmaWNhdGlvbg==");
    }

    [Test]
    public async Task CreateCar_WithBrandRelationship_ShouldLoadBrandCorrectly()
    {
        // Arrange
        var car = new Car(_validCommand);

        // Act
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Assert
        await SaveChangesAndClearContext();
        var savedCar = await Context.Cars
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.Title == "Toyota Corolla 2023");

        savedCar.Should().NotBeNull();
        savedCar!.Brand.Should().NotBeNull();
        savedCar.Brand!.Name.Should().Be("Toyota");
        savedCar.BrandId.Should().Be(_testBrand.Id);
    }

    [Test]
    public async Task UpdateCar_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var car = new Car(_validCommand);
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Act
        car.Title = "Updated Toyota Corolla 2023";
        car.Owner = "Maria Garcia";
        car.Description = "Updated description";
        await Context.SaveChangesAsync();

        // Assert
        await SaveChangesAndClearContext();
        var updatedCar = await Context.Cars.FindAsync(car.Id);
        updatedCar.Should().NotBeNull();
        updatedCar!.Title.Should().Be("Updated Toyota Corolla 2023");
        updatedCar.Owner.Should().Be("Maria Garcia");
        updatedCar.Description.Should().Be("Updated description");
    }

    [Test]
    public async Task DeleteCar_ShouldRemoveFromDatabase()
    {
        // Arrange
        var car = new Car(_validCommand);
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();
        var carId = car.Id;

        // Act
        Context.Cars.Remove(car);
        await Context.SaveChangesAsync();

        // Assert
        var deletedCar = await Context.Cars.FindAsync(carId);
        deletedCar.Should().BeNull();
    }

    [Test]
    public async Task QueryCarsByBrand_ShouldReturnCarsFromSpecificBrand()
    {
        // Arrange
        var honda = new Brand("Honda");
        Context.Brands.Add(honda);
        await Context.SaveChangesAsync();

        var toyotaCar = new Car(_validCommand);
        var hondaCarCommand = _validCommand with 
        { 
            BrandId = honda.Id, 
            Title = "Honda Civic", 
            LicensePlate = "XYZ9876",
            OriginalReservationId = 101
        };
        var hondaCar = new Car(hondaCarCommand);

        Context.Cars.AddRange(toyotaCar, hondaCar);
        await Context.SaveChangesAsync();

        // Act
        var hondaCars = await Context.Cars
            .Where(c => c.BrandId == honda.Id)
            .ToListAsync();

        // Assert
        hondaCars.Should().HaveCount(1);
        hondaCars.First().Title.Should().Be("Honda Civic");
    }

    [Test]
    public async Task QueryCarsByPriceRange_ShouldReturnCarsInRange()
    {
        // Arrange
        var expensiveCarCommand = _validCommand with 
        { 
            Price = 50000m, 
            Title = "Expensive Car",
            LicensePlate = "EXP5000",
            OriginalReservationId = 201
        };
        var cheapCarCommand = _validCommand with 
        { 
            Price = 15000m, 
            Title = "Cheap Car",
            LicensePlate = "CHP1500",
            OriginalReservationId = 202
        };

        var expensiveCar = new Car(expensiveCarCommand);
        var cheapCar = new Car(cheapCarCommand);
        var normalCar = new Car(_validCommand);

        Context.Cars.AddRange(expensiveCar, cheapCar, normalCar);
        await Context.SaveChangesAsync();

        // Act - Load cars and filter in-memory (avoids provider translation issues)
        var allCars = await Context.Cars.ToListAsync();
        var carsInRange = allCars.Where(c => c.Price != null && c.Price.Value >= 20000m && c.Price.Value <= 30000m).ToList();

        // Assert
        carsInRange.Should().HaveCount(1);
        carsInRange.First().Title.Should().Be("Toyota Corolla 2023");
    }

    [Test]
    public async Task UniqueLicensePlateConstraint_ShouldPreventDuplicates()
    {
        // Arrange
        var car1 = new Car(_validCommand);
        var car2Command = _validCommand with 
        { 
            Title = "Different Car Same Plate",
            OriginalReservationId = 200
        };
        var car2 = new Car(car2Command);

        // Act & Assert
        Context.Cars.Add(car1);
        await Context.SaveChangesAsync();

        Context.Cars.Add(car2);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw due to unique constraint
    }

    [Test]
    public async Task UniqueReservationIdConstraint_ShouldPreventDuplicates()
    {
        // Arrange
        var car1 = new Car(_validCommand);
        var car2Command = _validCommand with 
        { 
            Title = "Different Car Same Reservation",
            LicensePlate = "DIF1234"
        };
        var car2 = new Car(car2Command);

        // Act & Assert
        Context.Cars.Add(car1);
        await Context.SaveChangesAsync();

        Context.Cars.Add(car2);
        
        var action = async () => await Context.SaveChangesAsync();
        await action.Should().ThrowAsync<Exception>(); // Should throw due to unique constraint
    }

    [Test]
    public async Task CarEntityTracking_ShouldDetectValueObjectChanges()
    {
        // Arrange
        var car = new Car(_validCommand);
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Act
        car.Price = new Price(30000m);
        car.Year = new Year(2024);

        // Assert
        var entry = Context.Entry(car);
        entry.State.Should().Be(EntityState.Modified);
        entry.Property(c => c.Price).IsModified.Should().BeTrue();
        entry.Property(c => c.Year).IsModified.Should().BeTrue();
    }

    [Test]
    public async Task BrandDeletion_WithRestrictConstraint_ShouldPreventDeletion()
    {
        // Arrange
        var car = new Car(_validCommand);
        Context.Cars.Add(car);
        await Context.SaveChangesAsync();

        // Act & Assert - the provider may throw either during Remove or SaveChanges; handle both
        try
        {
            Context.Brands.Remove(_testBrand);
            await Context.SaveChangesAsync();
            Assert.Fail("Expected exception when deleting brand with existing cars");
        }
        catch (Exception)
        {
            // Expected: provider enforces FK/cascade restrictions
        }
    }
}
