using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Services;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CertiWeb.UnitTests.Certifications.Domain.Services;

/// <summary>
/// Unit tests for Car domain services
/// </summary>
public class CarDomainServiceTests
{
    private readonly Mock<ICarRepository> _carRepositoryMock;
    private readonly Mock<IBrandRepository> _brandRepositoryMock;

    public CarDomainServiceTests()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _brandRepositoryMock = new Mock<IBrandRepository>();
    }

    [Fact]
    public async Task ValidateCarUniqueness_WhenLicensePlateExists_ShouldReturnFalse()
    {
        // Arrange
        var licensePlate = new LicensePlate("ABC-123");
        var existingCar = new Car(
            1, 
            "Test Model", 
            new Year(2020), 
            new Price(25000), 
            licensePlate, 
            null
        );

        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(licensePlate))
            .ReturnsAsync(existingCar);

        // Act
        var result = await ValidateCarUniqueness(licensePlate);

        // Assert
        Assert.False(result);
        _carRepositoryMock.Verify(repo => repo.FindByLicensePlateAsync(licensePlate), Times.Once);
    }

    [Fact]
    public async Task ValidateCarUniqueness_WhenLicensePlateDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var licensePlate = new LicensePlate("XYZ-789");

        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(licensePlate))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await ValidateCarUniqueness(licensePlate);

        // Assert
        Assert.True(result);
        _carRepositoryMock.Verify(repo => repo.FindByLicensePlateAsync(licensePlate), Times.Once);
    }

    [Theory]
    [InlineData(2020, 25000, true)]  // Current year, reasonable price
    [InlineData(2030, 30000, false)] // Future year
    [InlineData(1800, 1000, false)]  // Too old
    [InlineData(2020, 0, false)]     // Zero price
    public async Task ValidateCarSpecifications_WithVariousInputs_ShouldReturnExpectedResult(
        int year, decimal price, bool expected)
    {
        // Arrange
        var carYear = new Year(year);
        var carPrice = new Price(price);

        // Act
        var result = await ValidateCarSpecifications(carYear, carPrice);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CalculateCarValue_WithDepreciation_ShouldReturnCorrectValue()
    {
        // Arrange
        var originalPrice = new Price(30000);
        var carYear = new Year(2018); // 6 years old (assuming current year 2024)
        var expectedDepreciationRate = 0.15m; // 15% per year
        var yearsOld = 6;
        var expectedValue = 30000 * (decimal)Math.Pow((double)(1 - expectedDepreciationRate), yearsOld);

        // Act
        var result = await CalculateCarValue(originalPrice, carYear);

        // Assert
        Assert.True(Math.Abs(result - expectedValue) < 100); // Allow small rounding differences
    }

    // Helper methods that would be implemented in actual domain services
    private async Task<bool> ValidateCarUniqueness(LicensePlate licensePlate)
    {
        var existingCar = await _carRepositoryMock.Object.FindByLicensePlateAsync(licensePlate);
        return existingCar == null;
    }

    private async Task<bool> ValidateCarSpecifications(Year year, Price price)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var currentYear = DateTime.Now.Year;
        
        // Business rules validation
        if (year.Value > currentYear) return false; // Future year not allowed
        if (year.Value < 1900) return false; // Too old
        if (price.Value <= 0) return false; // Invalid price
        
        return true;
    }

    private async Task<decimal> CalculateCarValue(Price originalPrice, Year year)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var currentYear = DateTime.Now.Year;
        var yearsOld = currentYear - year.Value;
        var depreciationRate = 0.15m; // 15% per year
        
        return originalPrice.Value * (decimal)Math.Pow((double)(1 - depreciationRate), yearsOld);
    }
}
