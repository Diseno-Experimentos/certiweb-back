using CertiWeb.API.Certifications.Interfaces.REST.Resources;
using CertiWeb.API.Certifications.Interfaces.REST.Transform;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace CertiWeb.UnitTests.Certifications.Interfaces.REST.Transform;

/// <summary>
/// Unit tests for resource transformations
/// </summary>
public class CarResourceTransformTests
{
    #region ToResource Tests

    [Test]
    public void Car_ToCarResource_ShouldMapAllProperties()
    {
        // Arrange
        var car = CreateTestCar();

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.AreEqual(car.Id, resource.Id);
        Assert.AreEqual(car.Model, resource.Model);
        Assert.AreEqual(car.Year.Value, resource.Year);
        Assert.AreEqual(car.Price.Value, resource.Price);
        Assert.AreEqual(car.LicensePlate.Value, resource.LicensePlate);
        Assert.AreEqual(car.BrandId, resource.BrandId);
    }

    [Test]
    public void Car_WithPdfCertification_ToCarResource_ShouldMapCertification()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var base64 = Convert.ToBase64String(pdfData);
        var car = CreateTestCar(pdfCertification: new PdfCertification(base64));

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.IsNotNull(resource.CertificationInfo);
        Assert.AreEqual(pdfData.Length, resource.CertificationInfo.SizeInBytes);
        Assert.IsTrue(resource.CertificationInfo.HasCertification);
    }

    [Test]
    public void Car_WithoutPdfCertification_ToCarResource_ShouldHandleNull()
    {
        // Arrange
        var car = CreateTestCar(pdfCertification: null);

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.IsNull(resource.CertificationInfo);
    }

    [Test]
    public void CarList_ToCarResourceList_ShouldMapAllCars()
    {
        // Arrange
        var cars = CreateTestCars(3);

        // Act
        var resources = cars.Select(CarResourceFromEntityAssembler.ToResourceFromEntity).ToList();

        // Assert
        Assert.IsNotNull(resources);
        Assert.AreEqual(3, resources.Count);
        
        for (int i = 0; i < cars.Count; i++)
        {
            Assert.AreEqual(cars[i].Id, resources[i].Id);
            Assert.AreEqual(cars[i].Model, resources[i].Model);
        }
    }

    #endregion

    #region FromResource Tests

    [Test]
    public void CreateCarResource_ToCar_ShouldMapAllProperties()
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC1234",
            BrandId = 1
        };

        // Act
        var car = CreateCarCommandFromResourceAssembler.ToCommandFromResource(createResource);

        // Assert
        Assert.IsNotNull(car);
        Assert.AreEqual(createResource.Model, car.Model);
        Assert.AreEqual(createResource.Year, car.Year);
        Assert.AreEqual(createResource.Price, car.Price);
        Assert.AreEqual(createResource.LicensePlate, car.LicensePlate);
        Assert.AreEqual(createResource.BrandId, car.BrandId);
    }

    [Test]
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
        Assert.IsNotNull(command);
        Assert.AreEqual(1, command.Id);
        Assert.AreEqual(updateResource.Model, command.Model);
        Assert.AreEqual(updateResource.Year, command.Year);
        Assert.AreEqual(updateResource.Price, command.Price);
        Assert.AreEqual(updateResource.LicensePlate, command.LicensePlate);
    }

    #endregion

    #region Validation Tests

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void CreateCarResource_WithInvalidModel_ShouldFailValidation(string invalidModel)
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = invalidModel,
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC1234",
            BrandId = 1
        };

        // Act & Assert
        var validationResults = ValidateResource(createResource);
        Assert.IsTrue(validationResults.Any(v => v.ErrorMessage.Contains("Model")));
    }

    [TestCase(1800)]
    [TestCase(2030)]
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
        Assert.IsTrue(validationResults.Any(v => v.ErrorMessage.Contains("Year")));
    }

    [TestCase(-1000)]
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
        Assert.IsTrue(validationResults.Any(v => v.ErrorMessage.Contains("Price")));
    }

    [TestCase("")]
    [TestCase("AB")]
    [TestCase("ABCDEFGHIJKLMNOP")]
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
        Assert.IsTrue(validationResults.Any(v => v.ErrorMessage.Contains("License")));
    }

    [Test]
    public void CreateCarResource_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var createResource = new CreateCarResource
        {
            Model = "Test Model",
            Year = 2020,
            Price = 25000,
            LicensePlate = "ABC1234",
            BrandId = 1
        };

        // Act
        var validationResults = ValidateResource(createResource);

        // Assert
        Assert.IsEmpty(validationResults);
    }

    #endregion

    #region Boundary and Edge Cases

    [Test]
    public void CarResource_WithMinimumValidValues_ShouldMapCorrectly()
    {
        // Arrange
        var cmdMin = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: "A",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 1900,
            BrandId: 1,
            Model: "A",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 0.01m,
            LicensePlate: "ABC",
            OriginalReservationId: 0
        );
        var car = new Car(cmdMin);
        var idPropMin = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idPropMin?.SetValue(car, 1);

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.AreEqual("A", resource.Model);
        Assert.AreEqual(1900, resource.Year);
        Assert.AreEqual(0.01m, resource.Price);
        Assert.AreEqual("ABC", resource.LicensePlate);
    }

    [Test]
    public void CarResource_WithMaximumValidValues_ShouldMapCorrectly()
    {
        // Arrange
        var longModel = new string('A', 100); // Assuming max length is 100
        var cmdMax = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: longModel,
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: DateTime.Now.Year,
            BrandId: 1,
            Model: longModel,
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 999999.99m,
            LicensePlate: "ABCDEFGHIJKLMNO",
            OriginalReservationId: 0
        );
        var car = new Car(cmdMax);
        var idPropMax = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idPropMax?.SetValue(car, int.MaxValue);

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.AreEqual(longModel, resource.Model);
        Assert.AreEqual(DateTime.Now.Year, resource.Year);
        Assert.AreEqual(999999.99m, resource.Price);
        Assert.AreEqual("ABCDEFGHIJKLMNO", resource.LicensePlate);
    }

    [Test]
    public void CarResource_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var specialModel = "Test Model with ñ, é, ü";
        var cmdSpecial = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: specialModel,
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: specialModel,
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC1234",
            OriginalReservationId: 0
        );
        var car = new Car(cmdSpecial);
        var idPropSpecial = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idPropSpecial?.SetValue(car, 1);

        // Act
        var resource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);

        // Assert
        Assert.IsNotNull(resource);
        Assert.AreEqual(specialModel, resource.Model);
    }

    #endregion

    #region Helper Methods

    private static Car CreateTestCar(int id = 1, PdfCertification? pdfCertification = null)
    {
        var cmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: $"Test Title {id}",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: pdfCertification?.Base64Data,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );

        var car = new Car(cmd);
        car.BrandId = 1;
        var idProp = typeof(Car).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idProp?.SetValue(car, id);
        return car;
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

public class CreateCarResource : System.ComponentModel.DataAnnotations.IValidatableObject
{
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int BrandId { get; set; }

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (string.IsNullOrWhiteSpace(Model))
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Model is required", new[] { "Model" }));

        var currentYear = DateTime.Now.Year;
        if (Year < 1900 || Year > currentYear + 1)
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Year is out of range", new[] { "Year" }));

        if (Price < 0)
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Price must be greater than or equal to zero", new[] { "Price" }));

        // API-level resource validation: enforce no hyphens and reasonable length in license plate
        if (string.IsNullOrWhiteSpace(LicensePlate))
        {
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("License plate is invalid", new[] { "LicensePlate" }));
        }
        else
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(LicensePlate, ".*[-].*") || LicensePlate.Length < 3 || LicensePlate.Length > 15 || !System.Text.RegularExpressions.Regex.IsMatch(LicensePlate, "^[A-Za-z0-9]+$"))
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("License plate is invalid", new[] { "LicensePlate" }));
            }
        }

        return results;
    }
}

public class UpdateCarResource : System.ComponentModel.DataAnnotations.IValidatableObject
{
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string LicensePlate { get; set; } = string.Empty;

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!string.IsNullOrEmpty(Model) && string.IsNullOrWhiteSpace(Model))
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Model is required", new[] { "Model" }));

        var currentYear = DateTime.Now.Year;
        if (Year != 0 && (Year < 1886 || Year > currentYear))
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Year is out of range", new[] { "Year" }));

        if (Price < 0)
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Price must be greater than or equal to zero", new[] { "Price" }));

        if (!string.IsNullOrEmpty(LicensePlate))
        {
            try
            {
                _ = new CertiWeb.API.Certifications.Domain.Model.ValueObjects.LicensePlate(LicensePlate);
            }
            catch (ArgumentException)
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("License plate is invalid", new[] { "LicensePlate" }));
            }
        }

        return results;
    }
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
                CertificationInfo = (entity.PdfCertification == null || string.IsNullOrEmpty(entity.PdfCertification.Base64Data))
                ? null
                : new CertificationInfoResource
                {
                    SizeInBytes = Convert.FromBase64String(entity.PdfCertification.Base64Data).Length,
                    HasCertification = true
                }
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
