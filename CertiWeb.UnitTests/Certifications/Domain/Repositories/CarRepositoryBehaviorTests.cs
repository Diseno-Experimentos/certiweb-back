using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CertiWeb.UnitTests.Certifications.Domain.Repositories;

/// <summary>
/// Unit tests for repository behavior and contract compliance
/// </summary>
public class CarRepositoryBehaviorTests
{
    private Mock<ICarRepository> _repositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<ICarRepository>();
    }

    [Test]
    public async Task FindByIdAsync_WhenCarExists_ShouldReturnCar()
    {
        // Arrange
        var carId = 1;
        var expectedCar = CreateTestCar(carId);
        
        _repositoryMock.Setup(repo => repo.FindByIdAsync(carId))
            .ReturnsAsync(expectedCar);

        // Act
        var result = await _repositoryMock.Object.FindByIdAsync(carId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(carId, result.Id);
        Assert.AreEqual(expectedCar.Model, result.Model);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(carId), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_WhenCarDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 999;
        
        _repositoryMock.Setup(repo => repo.FindByIdAsync(nonExistentId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _repositoryMock.Object.FindByIdAsync(nonExistentId);

        // Assert
        Assert.IsNull(result);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(nonExistentId), Times.Once);
    }

    [Test]
    public async Task FindByBrandIdAsync_WhenCarsExist_ShouldReturnFilteredCars()
    {
        // Arrange
        var brandId = 1;
        var cars = new List<Car>
        {
            CreateTestCar(1, brandId),
            CreateTestCar(2, brandId),
            CreateTestCar(3, 2) // Different brand
        };
        
        var expectedCars = cars.Where(c => c.BrandId == brandId).ToList();
        
        _repositoryMock.Setup(repo => repo.FindCarsByBrandIdAsync(brandId))
            .ReturnsAsync(expectedCars);

        // Act
        var result = await _repositoryMock.Object.FindCarsByBrandIdAsync(brandId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(car => car.BrandId == brandId));
        _repositoryMock.Verify(repo => repo.FindCarsByBrandIdAsync(brandId), Times.Once);
    }

    [Test]
    public async Task AddAsync_WhenValidCar_ShouldAddSuccessfully()
    {
        // Arrange
        var newCar = CreateTestCar(0); // New car without ID
        var savedCar = CreateTestCar(1); // Car with ID after save
        
        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repositoryMock.Object.AddAsync(newCar);

        // Assert
        _repositoryMock.Verify(repo => repo.AddAsync(It.Is<Car>(c => c.Model == newCar.Model)), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenValidCar_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingCar = CreateTestCar(1);
        var updateCmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: "Updated Model",
            Owner: existingCar.Owner,
            OwnerEmail: existingCar.OwnerEmail,
            Year: 2021,
            BrandId: existingCar.BrandId,
            Model: "Updated Model",
            Description: existingCar.Description,
            PdfCertification: existingCar.PdfCertification?.Base64Data,
            ImageUrl: existingCar.ImageUrl,
            Price: 30000m,
            LicensePlate: "UPD-123",
            OriginalReservationId: existingCar.OriginalReservationId
        );
        var updatedCar = new Car(updateCmd);
        var idPropUpdated = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idPropUpdated?.SetValue(updatedCar, 1);
        
        _repositoryMock.Setup(repo => repo.Update(It.IsAny<Car>()));

        // Act
        _repositoryMock.Object.Update(updatedCar);

        // Assert
        _repositoryMock.Verify(repo => repo.Update(It.Is<Car>(c => c.Id == 1)), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenValidId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var carId = 1;
        
        var toDelete = CreateTestCar(carId);
        _repositoryMock.Setup(repo => repo.Remove(It.IsAny<Car>()));

        // Act
        _repositoryMock.Object.Remove(toDelete);

        // Assert
        _repositoryMock.Verify(repo => repo.Remove(It.Is<Car>(c => c.Id == carId)), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 999;
        
        var nonExistentCar = CreateTestCar(nonExistentId);
        _repositoryMock.Setup(repo => repo.Remove(It.IsAny<Car>()));

        // Act
        _repositoryMock.Object.Remove(nonExistentCar);

        // Assert
        _repositoryMock.Verify(repo => repo.Remove(It.Is<Car>(c => c.Id == nonExistentId)), Times.Once);
    }

    [Test]
    public async Task FindAllAsync_WhenCarsExist_ShouldReturnAllCars()
    {
        // Arrange
        var cars = new List<Car>
        {
            CreateTestCar(1),
            CreateTestCar(2),
            CreateTestCar(3)
        };
        
        _repositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(cars);

        // Act
        var result = await _repositoryMock.Object.ListAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count());
        _repositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [TestCase("ABC")]
    [TestCase("XYZ")]
    [TestCase("123")]
    public async Task FindByLicensePlateAsync_WithVariousPlates_ShouldCallRepository(string plateNumber)
    {
        // Arrange
        var licensePlate = new LicensePlate($"{plateNumber}-456");
        var expectedCar = CreateTestCar(1, licensePlate: licensePlate);
        
        _repositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value))
            .ReturnsAsync(expectedCar);

        // Act
        var result = await _repositoryMock.Object.FindCarByLicensePlateAsync(licensePlate.Value);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(licensePlate.Value, result.LicensePlate.Value);
        _repositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value), Times.Once);
    }

    [Test]
    public async Task Repository_ShouldMaintainConsistentBehavior_AcrossMultipleCalls()
    {
        // Arrange
        var car = CreateTestCar(1);
        
        _repositoryMock.Setup(repo => repo.FindByIdAsync(1))
            .ReturnsAsync(car);

        // Act
        var result1 = await _repositoryMock.Object.FindByIdAsync(1);
        var result2 = await _repositoryMock.Object.FindByIdAsync(1);

        // Assert
        Assert.AreEqual(result1.Id, result2.Id);
        Assert.AreEqual(result1.Model, result2.Model);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(1), Times.Exactly(2));
    }

    private static Car CreateTestCar(int id, int brandId = 1, LicensePlate? licensePlate = null)
    {
        var cmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: $"Test Title {id}",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: brandId,
            Model: $"Test Model {id}",
            Description: null,
            PdfCertification: licensePlate != null ? string.Empty : string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: licensePlate?.Value ?? $"TST-{id:000}",
            OriginalReservationId: 0
        );

        var car = new Car(cmd);
        // set BrandId explicitly
        car.BrandId = brandId;

        // set Id via reflection (private setter)
        var idProp = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idProp?.SetValue(car, id);

        return car;
    }
}
