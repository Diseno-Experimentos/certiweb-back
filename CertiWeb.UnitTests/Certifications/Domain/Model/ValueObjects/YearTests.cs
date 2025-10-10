using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

[TestFixture]
public class YearTests
{
    [Test]
    public void Constructor_WithValidYear_ShouldCreateYearSuccessfully()
    {
        // Arrange
        var validYear = 2023;

        // Act
        var year = new Year(validYear);

        // Assert
        year.Value.Should().Be(validYear);
    }

    [Test]
    public void Constructor_WithCurrentYear_ShouldCreateYearSuccessfully()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;

        // Act
        var year = new Year(currentYear);

        // Assert
        year.Value.Should().Be(currentYear);
    }

    [Test]
    public void Constructor_WithNextYear_ShouldCreateYearSuccessfully()
    {
        // Arrange
        var nextYear = DateTime.Now.Year + 1;

        // Act
        var year = new Year(nextYear);

        // Assert
        year.Value.Should().Be(nextYear);
    }

    [Test]
    public void Constructor_WithYearBefore1900_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidYear = 1899;

        // Act & Assert
        var action = () => new Year(invalidYear);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Year must be between 1900 and * (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithYearAfterNextYear_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidYear = DateTime.Now.Year + 2;

        // Act & Assert
        var action = () => new Year(invalidYear);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Year must be between 1900 and * (Parameter 'value')");
    }

    [Test]
    public void ImplicitConversion_FromYearToInt_ShouldReturnCorrectValue()
    {
        // Arrange
        var year = new Year(2023);

        // Act
        int value = year;

        // Assert
        value.Should().Be(2023);
    }

    [Test]
    public void ImplicitConversion_FromIntToYear_ShouldCreateYearCorrectly()
    {
        // Arrange
        var value = 2023;

        // Act
        Year year = value;

        // Assert
        year.Value.Should().Be(2023);
    }

    [TestCase(1900)]
    [TestCase(1950)]
    [TestCase(2000)]
    [TestCase(2023)]
    public void Constructor_WithBoundaryValidYears_ShouldCreateYearSuccessfully(int validYear)
    {
        // Arrange & Act
        var year = new Year(validYear);

        // Assert
        year.Value.Should().Be(validYear);
    }
}
