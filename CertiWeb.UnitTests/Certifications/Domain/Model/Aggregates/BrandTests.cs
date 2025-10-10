using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.Aggregates;

[TestFixture]
public class BrandTests
{
    [Test]
    public void DefaultConstructor_ShouldCreateBrandWithEmptyNameAndActiveStatus()
    {
        // Arrange & Act
        var brand = new Brand();

        // Assert
        brand.Id.Should().Be(0);
        brand.Name.Should().Be(string.Empty);
        brand.IsActive.Should().BeTrue();
    }

    [Test]
    public void Constructor_WithValidName_ShouldCreateBrandSuccessfully()
    {
        // Arrange
        var brandName = "Toyota";

        // Act
        var brand = new Brand(brandName);

        // Assert
        brand.Name.Should().Be(brandName);
        brand.IsActive.Should().BeTrue();
        brand.Id.Should().Be(0); // Default value for new entity
    }

    [Test]
    public void Constructor_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyName = "";

        // Act & Assert
        var action = () => new Brand(emptyName);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Brand name cannot be empty (Parameter 'name')");
    }

    [Test]
    public void Constructor_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullName = null;

        // Act & Assert
        var action = () => new Brand(nullName!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Brand name cannot be empty (Parameter 'name')");
    }

    [Test]
    public void Constructor_WithWhitespaceOnly_ShouldThrowArgumentException()
    {
        // Arrange
        var whitespaceName = "   ";

        // Act & Assert
        var action = () => new Brand(whitespaceName);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Brand name cannot be empty (Parameter 'name')");
    }

    [Test]
    public void Name_Setter_WithValidName_ShouldUpdateNameSuccessfully()
    {
        // Arrange
        var brand = new Brand("Honda");
        var newName = "Toyota";

        // Act
        brand.Name = newName;

        // Assert
        brand.Name.Should().Be(newName);
    }

    [Test]
    public void IsActive_Setter_ShouldUpdateStatusSuccessfully()
    {
        // Arrange
        var brand = new Brand("Nissan");

        // Act
        brand.IsActive = false;

        // Assert
        brand.IsActive.Should().BeFalse();
    }

    [Test]
    public void Id_Setter_ShouldUpdateIdSuccessfully()
    {
        // Arrange
        var brand = new Brand("Ford");
        var newId = 123;

        // Act
        brand.Id = newId;

        // Assert
        brand.Id.Should().Be(newId);
    }

    [TestCase("BMW")]
    [TestCase("Mercedes-Benz")]
    [TestCase("Volkswagen")]
    [TestCase("Audi")]
    [TestCase("Hyundai")]
    public void Constructor_WithVariousValidNames_ShouldCreateBrandSuccessfully(string brandName)
    {
        // Arrange & Act
        var brand = new Brand(brandName);

        // Assert
        brand.Name.Should().Be(brandName);
        brand.IsActive.Should().BeTrue();
    }

    [Test]
    public void Constructor_WithLongValidName_ShouldCreateBrandSuccessfully()
    {
        // Arrange
        var longBrandName = "A Very Long Brand Name That Still Should Be Valid";

        // Act
        var brand = new Brand(longBrandName);

        // Assert
        brand.Name.Should().Be(longBrandName);
        brand.IsActive.Should().BeTrue();
    }

    [Test]
    public void Constructor_WithSpecialCharacters_ShouldCreateBrandSuccessfully()
    {
        // Arrange
        var brandNameWithSpecialChars = "Mercedes-Benz & Co.";

        // Act
        var brand = new Brand(brandNameWithSpecialChars);

        // Assert
        brand.Name.Should().Be(brandNameWithSpecialChars);
        brand.IsActive.Should().BeTrue();
    }
}
