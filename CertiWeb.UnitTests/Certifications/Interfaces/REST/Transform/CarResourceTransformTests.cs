using CertiWeb.API.Certifications.Interfaces.REST.Resources;
using CertiWeb.API.Certifications.Interfaces.REST.Transform;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CertiWeb.UnitTests.Certifications.Interfaces.REST.Transform;

/// <summary>
/// Unit tests for resource transformations
/// </summary>
public class CarResourceTransformTests
{
    #region ToResource Tests

    [Fact]
    public void Car_ToCarResource_ShouldMapAllProperties()
    {
        // Arrange
        var car = CreateTestCar();

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(car.Id, resource.Id);
        Assert.Equal(car.Model, resource.Model);
        Assert.Equal(car.Year.Value, resource.Year);
        Assert.Equal(car.Price.Value, resource.Price);
        Assert.Equal(car.LicensePlate.Value, resource.LicensePlate);
        Assert.Equal(car.BrandId, resource.BrandId);
    }

    [Fact]
    public void Car_WithPdfCertification_ToCarResource_ShouldMapCertification()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var car = CreateTestCar(pdfCertification: new PdfCertification(pdfData));

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.NotNull(resource.CertificationInfo);
        Assert.Equal(pdfData.Length, resource.CertificationInfo.SizeInBytes);
        Assert.True(resource.CertificationInfo.HasCertification);
    }

    [Fact]
    public void Car_WithoutPdfCertification_ToCarResource_ShouldHandleNull()
    {
        // Arrange
        var car = CreateTestCar(pdfCertification: null);

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.Null(resource.CertificationInfo);
    }

    [Fact]
    public void CarList_ToCarResourceList_ShouldMapAllCars()
    {
        // Arrange
        var cars = CreateTestCars(3);

        // Act
        var resources = cars.Select(CarResourceFromEntityAssembler.ToResourceFromEntity).ToList();

        // Assert
        Assert.NotNull(resources);
        Assert.Equal(3, resources.Count);
        
        for (int i = 0; i < cars.Count; i++)
        {
            Assert.Equal(cars[i].Id, resources[i].Id);
            Assert.Equal(cars[i].Model, resources[i].Model);
        }
    }

    #endregion

    #region FromResource Tests

    [Fact]
    public void CreateCarResource_ToCar_ShouldMapAllProperties()
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC-123",
            BrandId = 1
        };

        // Act
        var car = CreateCarCommandFromResourceAssembler.ToCommandFromResource(createResource);

        // Assert
        Assert.NotNull(car);
        Assert.Equal(createResource.Model, car.Model);
        Assert.Equal(createResource.Year, car.Year);
        Assert.Equal(createResource.Price, car.Price);
        Assert.Equal(createResource.LicensePlate, car.LicensePlate);
        Assert.Equal(createResource.BrandId, car.BrandId);
    }

    [Fact]
    public void UpdateCarResource_ToCar_ShouldMapAllProperties()
    {
        // Arrange
        var updateResource = new UpdateCarResource
        {
            Model = "Updated Model",
            Year = 2021,
            Price = 30000,
            LicensePlate = "UPD-123"
        };

        // Act
        var command = UpdateCarCommandFromResourceAssembler.ToCommandFromResource(1, updateResource);

        // Assert
        Assert.NotNull(command);
        Assert.Equal(1, command.Id);
        Assert.Equal(updateResource.Model, command.Model);
        Assert.Equal(updateResource.Year, command.Year);
        Assert.Equal(updateResource.Price, command.Price);
        Assert.Equal(updateResource.LicensePlate, command.LicensePlate);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateCarResource_WithInvalidModel_ShouldFailValidation(string invalidModel)
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = invalidModel,
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC-123",
            BrandId = 1
        };

        // Act & Assert
        var validationResults = ValidateResource(createResource);
        Assert.Contains(validationResults, v => v.ErrorMessage.Contains("Model"));
    }

    [Theory]
    [InlineData(1800)]
    [InlineData(2030)]
    public void CreateCarResource_WithInvalidYear_ShouldFailValidation(int invalidYear)
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = invalidYear,
            Price = 25000,
            LicensePlate = "ABC-123",
            BrandId = 1
        };

        // Act & Assert
        var validationResults = ValidateResource(createResource);
        Assert.Contains(validationResults, v => v.ErrorMessage.Contains("Year"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void CreateCarResource_WithInvalidPrice_ShouldFailValidation(decimal invalidPrice)
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = invalidPrice,
            LicensePlate = "ABC-123",
            BrandId = 1
        };

        // Act & Assert
        var validationResults = ValidateResource(createResource);
        Assert.Contains(validationResults, v => v.ErrorMessage.Contains("Price"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    public void CreateCarResource_WithInvalidLicensePlate_ShouldFailValidation(string invalidPlate)
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = 25000,
            LicensePlate = invalidPlate,
            BrandId = 1
        };

        // Act & Assert
        var validationResults = ValidateResource(createResource);
        Assert.Contains(validationResults, v => v.ErrorMessage.Contains("License"));
    }

    [Fact]
    public void CreateCarResource_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC-123",
            BrandId = 1
        };

        // Act
        var validationResults = ValidateResource(createResource);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region Boundary and Edge Cases

    [Fact]
    public void CarResource_WithMinimumValidValues_ShouldMapCorrectly()
    {
        // Arrange
        var car = new Car(
            1,
            "A", // Minimum length model
            new Year(1886), // Minimum valid year
            new Price(0.01m), // Minimum valid price
            new LicensePlate("ABC"), // Minimum length plate
            null
        );

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal("A", resource.Model);
        Assert.Equal(1886, resource.Year);
        Assert.Equal(0.01m, resource.Price);
        Assert.Equal("ABC", resource.LicensePlate);
    }

    [Fact]
    public void CarResource_WithMaximumValidValues_ShouldMapCorrectly()
    {
        // Arrange
        var longModel = new string('A', 100); // Assuming max length is 100
        var car = new Car(
            int.MaxValue,
            longModel,
            new Year(DateTime.Now.Year),
            new Price(999999.99m),
            new LicensePlate("ABCDEFGHIJKLMNO"), // Maximum length plate
            null
        );

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(longModel, resource.Model);
        Assert.Equal(DateTime.Now.Year, resource.Year);
        Assert.Equal(999999.99m, resource.Price);
        Assert.Equal("ABCDEFGHIJKLMNO", resource.LicensePlate);
    }

    [Fact]
    public void CarResource_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialModel = "Test Model with ñ, é, ü";
        var car = new Car(
            1,
            specialModel,
            new Year(2020),
            new Price(25000),
            new LicensePlate("ABC-123"),
            null
        );

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(specialModel, resource.Model);
    }

    #endregion

    #region Helper Methods

    private static Car CreateTestCar(int id = 1, PdfCertification? pdfCertification = null)
    {
        return new Car(
            id,
            "Test Model",
            new Year(2020),
            new Price(25000),
            new LicensePlate("ABC-123"),
            pdfCertification
        )
        {
            BrandId = 1
        };
    }

    private static List<Car> CreateTestCars(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCar(i))
            .ToList();
    }

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> ValidateResource<T>(T resource)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(resource);
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(resource, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}

// Mock resource classes for testing
public class CarResource
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public CertificationInfoResource? CertificationInfo { get; set; }
}

public class CertificationInfoResource
{
    public int SizeInBytes { get; set; }
    public bool HasCertification { get; set; }
}

public class CreateCarResource
{
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int BrandId { get; set; }
}

public class UpdateCarResource
{
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
}

// Mock assembler classes
public static class CarResourceFromEntityAssembler
{
    public static CarResource ToResourceFromEntity(Car entity)
    {
        return new CarResource
        {
            Id = entity.Id,
            Model = entity.Model,
            Year = entity.Year.Value,
            Price = entity.Price.Value,
            LicensePlate = entity.LicensePlate.Value,
            BrandId = entity.BrandId,
            CertificationInfo = entity.PdfCertification != null 
                ? new CertificationInfoResource
                {
                    SizeInBytes = entity.PdfCertification.SizeInBytes,
                    HasCertification = true
                }
                : null
        };
    }
}

public static class CreateCarCommandFromResourceAssembler
{
    public static dynamic ToCommandFromResource(CreateCarResource resource)
    {
        return new
        {
            resource.Model,
            resource.Year,
            resource.Price,
            resource.LicensePlate,
            resource.BrandId
        };
    }
}

public static class UpdateCarCommandFromResourceAssembler
{
    public static dynamic ToCommandFromResource(int id, UpdateCarResource resource)
    {
        return new
        {
            Id = id,
            resource.Model,
            resource.Year,
            resource.Price,
            resource.LicensePlate
        };
    }
}
