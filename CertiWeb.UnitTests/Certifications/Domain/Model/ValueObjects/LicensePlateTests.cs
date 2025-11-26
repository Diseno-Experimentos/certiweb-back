using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

[TestFixture]
public class LicensePlateTests
{
    [Test]
    public void Constructor_WithValidLicensePlate_ShouldCreateLicensePlateSuccessfully()
    {
        // Arrange
        var validPlate = "ABC1234";

        // Act
        var licensePlate = new LicensePlate(validPlate);

        // Assert
        licensePlate.Value.Should().Be("ABC1234");
    }

    [Test]
    public void Constructor_WithLowercaseLicensePlate_ShouldConvertToUppercase()
    {
        // Arrange
        var lowercasePlate = "abc1234";

        // Act
        var licensePlate = new LicensePlate(lowercasePlate);

        // Assert
        licensePlate.Value.Should().Be("ABC1234");
    }

    [Test]
    public void Constructor_WithMixedCaseLicensePlate_ShouldConvertToUppercase()
    {
        // Arrange
        var mixedCasePlate = "AbC123d";

        // Act
        var licensePlate = new LicensePlate(mixedCasePlate);

        // Assert
        licensePlate.Value.Should().Be("ABC123D");
    }

    [Test]
    public void Constructor_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyPlate = "";

        // Act & Assert
        var action = () => new LicensePlate(emptyPlate);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate cannot be null or whitespace (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullPlate = null;

        // Act & Assert
        var action = () => new LicensePlate(nullPlate!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate cannot be null or whitespace (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithWhitespaceOnly_ShouldThrowArgumentException()
    {
        // Arrange
        var whitespacePlate = "   ";

        // Act & Assert
        var action = () => new LicensePlate(whitespacePlate);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate cannot be null or whitespace (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithTooShortPlate_ShouldThrowArgumentException()
    {
        // Arrange
        var shortPlate = "AB123"; // 5 characters

        // Act & Assert
        var action = () => new LicensePlate(shortPlate);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate must be between 3 and 15 characters (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithTooLongPlate_ShouldThrowArgumentException()
    {
        // Arrange
        var longPlate = "ABCDEFGH123"; // 11 characters

        // Act & Assert
        var action = () => new LicensePlate(longPlate);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate must be between 3 and 15 characters (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithMinimumValidLength_ShouldCreateLicensePlateSuccessfully()
    {
        // Arrange
        var minimumPlate = "ABC"; // 3 characters

        // Act
        var licensePlate = new LicensePlate(minimumPlate);

        // Assert
        licensePlate.Value.Should().Be("ABC");
    }

    [Test]
    public void Constructor_WithMaximumValidLength_ShouldCreateLicensePlateSuccessfully()
    {
        // Arrange
        var maximumPlate = "ABCDEFGHIJKLMNO"; // 15 characters

        // Act
        var licensePlate = new LicensePlate(maximumPlate);

        // Assert
        licensePlate.Value.Should().Be("ABCDEFGHIJKLMNO");
    }

    [Test]
    public void ImplicitConversion_FromLicensePlateToString_ShouldReturnCorrectValue()
    {
        // Arrange
        var licensePlate = new LicensePlate("XYZ9876");

        // Act
        string value = licensePlate;

        // Assert
        value.Should().Be("XYZ9876");
    }

    [Test]
    public void ImplicitConversion_FromStringToLicensePlate_ShouldCreateLicensePlateCorrectly()
    {
        // Arrange
        var plateValue = "DEF4567";

        // Act
        LicensePlate licensePlate = plateValue;

        // Assert
        licensePlate.Value.Should().Be("DEF4567");
    }

    [TestCase("ABC123")]
    [TestCase("XYZ987")]
    [TestCase("PERU123")]
    [TestCase("LIM1234")]
    [TestCase("ABCD123456")]
    public void Constructor_WithValidVariousFormats_ShouldCreateLicensePlateSuccessfully(string plate)
    {
        // Arrange & Act
        var licensePlate = new LicensePlate(plate);

        // Assert
        licensePlate.Value.Should().Be(plate.ToUpperInvariant());
    }
}
