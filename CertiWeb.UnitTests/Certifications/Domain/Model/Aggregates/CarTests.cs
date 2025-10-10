using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.Aggregates;

[TestFixture]
public class CarTests
{
    private CreateCarCommand _validCommand;

    [SetUp]
    public void SetUp()
    {
        _validCommand = new CreateCarCommand(
            Title: "Toyota Corolla 2023",
            Owner: "Juan Perez",
            OwnerEmail: "juan.perez@email.com",
            Year: 2023,
            BrandId: 1,
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
    public void DefaultConstructor_ShouldCreateCarWithEmptyProperties()
    {
        // Arrange & Act
        var car = new Car();

        // Assert
        car.Id.Should().Be(0);
        car.Title.Should().Be(string.Empty);
        car.Owner.Should().Be(string.Empty);
        car.OwnerEmail.Should().Be(string.Empty);
        car.Model.Should().Be(string.Empty);
        car.Description.Should().BeNull();
        car.ImageUrl.Should().BeNull();
        car.BrandId.Should().Be(0);
        car.Brand.Should().BeNull();
        car.OriginalReservationId.Should().Be(0);
    }

    [Test]
    public void Constructor_WithValidCommand_ShouldCreateCarSuccessfully()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var car = new Car(command);

        // Assert
        car.Title.Should().Be(command.Title);
        car.Owner.Should().Be(command.Owner);
        car.OwnerEmail.Should().Be(command.OwnerEmail);
        car.Year.Value.Should().Be(command.Year);
        car.BrandId.Should().Be(command.BrandId);
        car.Model.Should().Be(command.Model);
        car.Description.Should().Be(command.Description);
        car.PdfCertification.Base64Data.Should().Be(command.PdfCertification);
        car.ImageUrl.Should().Be(command.ImageUrl);
        car.Price.Value.Should().Be(command.Price);
        car.LicensePlate.Value.Should().Be(command.LicensePlate.ToUpperInvariant());
        car.OriginalReservationId.Should().Be(command.OriginalReservationId);
    }

    [Test]
    public void Constructor_WithCommandContainingNullDescription_ShouldCreateCarWithNullDescription()
    {
        // Arrange
        var command = _validCommand with { Description = null };

        // Act
        var car = new Car(command);

        // Assert
        car.Description.Should().BeNull();
        car.Title.Should().Be(command.Title);
        car.Owner.Should().Be(command.Owner);
    }

    [Test]
    public void Constructor_WithCommandContainingNullImageUrl_ShouldCreateCarWithNullImageUrl()
    {
        // Arrange
        var command = _validCommand with { ImageUrl = null };

        // Act
        var car = new Car(command);

        // Assert
        car.ImageUrl.Should().BeNull();
        car.Title.Should().Be(command.Title);
        car.Model.Should().Be(command.Model);
    }

    [Test]
    public void Constructor_WithValidYearValueObject_ShouldCreateYearCorrectly()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var car = new Car(command);

        // Assert
        car.Year.Should().NotBeNull();
        car.Year.Value.Should().Be(command.Year);
    }

    [Test]
    public void Constructor_WithValidPriceValueObject_ShouldCreatePriceCorrectly()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var car = new Car(command);

        // Assert
        car.Price.Should().NotBeNull();
        car.Price.Value.Should().Be(command.Price);
        car.Price.Currency.Should().Be("SOL"); // Default currency
    }

    [Test]
    public void Constructor_WithValidLicensePlateValueObject_ShouldCreateLicensePlateCorrectly()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var car = new Car(command);

        // Assert
        car.LicensePlate.Should().NotBeNull();
        car.LicensePlate.Value.Should().Be(command.LicensePlate.ToUpperInvariant());
    }

    [Test]
    public void Constructor_WithValidPdfCertificationValueObject_ShouldCreatePdfCertificationCorrectly()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var car = new Car(command);

        // Assert
        car.PdfCertification.Should().NotBeNull();
        car.PdfCertification.Base64Data.Should().Be(command.PdfCertification);
    }

    [Test]
    public void Constructor_WithInvalidYear_ShouldThrowArgumentException()
    {
        // Arrange
        var command = _validCommand with { Year = 1800 }; // Invalid year

        // Act & Assert
        var action = () => new Car(command);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Year must be between 1900 and * (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithInvalidPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var command = _validCommand with { Price = -1000 }; // Negative price

        // Act & Assert
        var action = () => new Car(command);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Price cannot be negative (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithInvalidLicensePlate_ShouldThrowArgumentException()
    {
        // Arrange
        var command = _validCommand with { LicensePlate = "AB" }; // Too short

        // Act & Assert
        var action = () => new Car(command);
        action.Should().Throw<ArgumentException>()
            .WithMessage("License plate must be between 6 and 10 characters (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithInvalidPdfCertification_ShouldThrowArgumentException()
    {
        // Arrange
        var command = _validCommand with { PdfCertification = "ABC" }; // Too short

        // Act & Assert
        var action = () => new Car(command);
        action.Should().Throw<ArgumentException>()
            .WithMessage("PDF certification data is too short (minimum 10 characters) (Parameter 'base64Data')");
    }

    [Test]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var car = new Car();
        var newTitle = "Honda Civic 2024";
        var newOwner = "Maria Garcia";
        var newEmail = "maria.garcia@email.com";
        var newModel = "Civic";
        var newDescription = "Updated description";
        var newImageUrl = "https://example.com/new-image.jpg";
        var newBrandId = 2;
        var newReservationId = 200;

        // Act
        car.Title = newTitle;
        car.Owner = newOwner;
        car.OwnerEmail = newEmail;
        car.Model = newModel;
        car.Description = newDescription;
        car.ImageUrl = newImageUrl;
        car.BrandId = newBrandId;
        car.OriginalReservationId = newReservationId;

        // Assert
        car.Title.Should().Be(newTitle);
        car.Owner.Should().Be(newOwner);
        car.OwnerEmail.Should().Be(newEmail);
        car.Model.Should().Be(newModel);
        car.Description.Should().Be(newDescription);
        car.ImageUrl.Should().Be(newImageUrl);
        car.BrandId.Should().Be(newBrandId);
        car.OriginalReservationId.Should().Be(newReservationId);
    }

    [Test]
    public void Brand_Navigation_Property_ShouldAllowSettingBrand()
    {
        // Arrange
        var car = new Car();
        var brand = new Brand("Toyota");

        // Act
        car.Brand = brand;

        // Assert
        car.Brand.Should().Be(brand);
        car.Brand.Name.Should().Be("Toyota");
    }
}
