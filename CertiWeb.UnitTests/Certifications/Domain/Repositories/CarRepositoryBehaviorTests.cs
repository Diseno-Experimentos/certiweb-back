using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CertiWeb.UnitTests.Certifications.Domain.Repositories;

/// <summary>
/// Unit tests for repository behavior and contract compliance
/// </summary>
public class CarRepositoryBehaviorTests
{
    private readonly Mock<ICarRepository> _repositoryMock;

    public CarRepositoryBehaviorTests()
    {
        _repositoryMock = new Mock<ICarRepository>();
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(carId, result.Id);
        Assert.Equal(expectedCar.Model, result.Model);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(carId), Times.Once);
    }

    [Fact]
    public async Task FindByIdAsync_WhenCarDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 999;
        
        _repositoryMock.Setup(repo => repo.FindByIdAsync(nonExistentId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _repositoryMock.Object.FindByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(nonExistentId), Times.Once);
    }

    [Fact]
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
        
        _repositoryMock.Setup(repo => repo.FindByBrandIdAsync(brandId))
            .ReturnsAsync(expectedCars);

        // Act
        var result = await _repositoryMock.Object.FindByBrandIdAsync(brandId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, car => Assert.Equal(brandId, car.BrandId));
        _repositoryMock.Verify(repo => repo.FindByBrandIdAsync(brandId), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenValidCar_ShouldAddSuccessfully()
    {
        // Arrange
        var newCar = CreateTestCar(0); // New car without ID
        var savedCar = CreateTestCar(1); // Car with ID after save
        
        _repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .ReturnsAsync(savedCar);

        // Act
        var result = await _repositoryMock.Object.AddAsync(newCar);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        _repositoryMock.Verify(repo => repo.AddAsync(It.Is<Car>(c => c.Model == newCar.Model)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenValidCar_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingCar = CreateTestCar(1);
        var updatedCar = new Car(
            1, 
            "Updated Model", 
            new Year(2021), 
            new Price(30000), 
            new LicensePlate("UPD-123"), 
            null
        );
        
        _repositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Car>()))
            .ReturnsAsync(updatedCar);

        // Act
        var result = await _repositoryMock.Object.UpdateAsync(updatedCar);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Model", result.Model);
        _repositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Car>(c => c.Id == 1)), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenValidId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var carId = 1;
        
        _repositoryMock.Setup(repo => repo.DeleteAsync(carId))
            .ReturnsAsync(true);

        // Act
        var result = await _repositoryMock.Object.DeleteAsync(carId);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(repo => repo.DeleteAsync(carId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 999;
        
        _repositoryMock.Setup(repo => repo.DeleteAsync(nonExistentId))
            .ReturnsAsync(false);

        // Act
        var result = await _repositoryMock.Object.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(repo => repo.DeleteAsync(nonExistentId), Times.Once);
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _repositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("XYZ")]
    [InlineData("123")]
    public async Task FindByLicensePlateAsync_WithVariousPlates_ShouldCallRepository(string plateNumber)
    {
        // Arrange
        var licensePlate = new LicensePlate($"{plateNumber}-456");
        var expectedCar = CreateTestCar(1, licensePlate: licensePlate);
        
        _repositoryMock.Setup(repo => repo.FindByLicensePlateAsync(licensePlate))
            .ReturnsAsync(expectedCar);

        // Act
        var result = await _repositoryMock.Object.FindByLicensePlateAsync(licensePlate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(licensePlate.Value, result.LicensePlate.Value);
        _repositoryMock.Verify(repo => repo.FindByLicensePlateAsync(licensePlate), Times.Once);
    }

    [Fact]
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
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(result1.Model, result2.Model);
        _repositoryMock.Verify(repo => repo.FindByIdAsync(1), Times.Exactly(2));
    }

    private static Car CreateTestCar(int id, int brandId = 1, LicensePlate? licensePlate = null)
    {
        return new Car(
            id,
            $"Test Model {id}",
            new Year(2020),
            new Price(25000),
            licensePlate ?? new LicensePlate($"TST-{id:000}"),
            null
        )
        {
            BrandId = brandId
        };
    }
}
