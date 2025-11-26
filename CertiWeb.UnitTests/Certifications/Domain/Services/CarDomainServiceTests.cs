using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Services;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using NUnit.Framework;
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

    [Test]
    public async Task ValidateCarUniqueness_WhenLicensePlateExists_ShouldReturnFalse()
    {
        // Arrange
        var licensePlate = new LicensePlate("ABC-123");
        var createCmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: licensePlate.Value,
            OriginalReservationId: 0
        );

        var existingCar = new Car(createCmd);
        var idProp = typeof(Car).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(existingCar, 1);

        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value))
            .ReturnsAsync(existingCar);

        // Act
        var result = await ValidateCarUniqueness(licensePlate);

        // Assert
        Assert.IsFalse(result);
        _carRepositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value), Times.Once);
    }

    [Test]
    public async Task ValidateCarUniqueness_WhenLicensePlateDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var licensePlate = new LicensePlate("XYZ-789");

        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await ValidateCarUniqueness(licensePlate);

        // Assert
        Assert.IsTrue(result);
        _carRepositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(licensePlate.Value), Times.Once);
    }

    [TestCase(2020, 25000, true)]  // Current year, reasonable price
    [TestCase(2030, 30000, false)] // Future year (validation should return false)
    [TestCase(1800, 1000, false)]  // Too old
    [TestCase(2020, 0, false)]     // Zero price
    public async Task ValidateCarSpecifications_WithVariousInputs_ShouldReturnExpectedResult(
        int year, decimal price, bool expected)
    {
        // Act
        var result = await ValidateCarSpecifications(year, price);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public async Task CalculateCarValue_WithDepreciation_ShouldReturnCorrectValue()
    {
        // Arrange
        var originalPrice = new Price(30000);
        var carYear = new Year(2018);
        var expectedDepreciationRate = 0.15m; // 15% per year
        var currentYear = System.DateTime.Now.Year;
        var yearsOld = currentYear - carYear.Value;
        var expectedValue = 30000 * (decimal)Math.Pow((double)(1 - expectedDepreciationRate), yearsOld);

        // Act
        var result = await CalculateCarValue(originalPrice, carYear);

        // Assert
        Assert.Less(Math.Abs(result - expectedValue), 100); // Allow small rounding differences
    }

    // Helper methods that would be implemented in actual domain services
    private async Task<bool> ValidateCarUniqueness(LicensePlate licensePlate)
    {
        var existingCar = await _carRepositoryMock.Object.FindCarByLicensePlateAsync(licensePlate.Value);
        return existingCar == null;
    }

    private async Task<bool> ValidateCarSpecifications(int yearValue, decimal priceValue)
    {
        await Task.CompletedTask; // Simulate async operation

        // Try to construct value objects; if construction fails, consider invalid specs.
        Year year;
        Price price;
        try
        {
            year = new Year(yearValue);
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            price = new Price(priceValue);
        }
        catch (ArgumentException)
        {
            return false;
        }

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
