using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using Xunit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

/// <summary>
/// Advanced validation tests for value objects
/// </summary>
public class ValueObjectValidationTests
{
    #region Year Validation Tests

    [Theory]
    [InlineData(1885)] // Before first car
    [InlineData(2050)] // Future year
    [InlineData(0)]
    [InlineData(-1)]
    public void Year_WhenInvalidRange_ShouldThrowArgumentException(int invalidYear)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Year(invalidYear));
        Assert.Contains("Year must be between", exception.Message);
    }

    [Theory]
    [InlineData(1886)] // First car year
    [InlineData(2024)] // Current year
    [InlineData(2000)] // Valid year
    public void Year_WhenValidRange_ShouldCreateSuccessfully(int validYear)
    {
        // Act
        var year = new Year(validYear);

        // Assert
        Assert.Equal(validYear, year.Value);
    }

    [Fact]
    public void Year_WhenCurrentYear_ShouldBeValid()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;

        // Act
        var year = new Year(currentYear);

        // Assert
        Assert.Equal(currentYear, year.Value);
    }

    #endregion

    #region Price Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void Price_WhenNegative_ShouldThrowArgumentException(decimal negativePrice)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Price(negativePrice));
        Assert.Contains("Price must be greater than zero", exception.Message);
    }

    [Fact]
    public void Price_WhenZero_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Price(0));
        Assert.Contains("Price must be greater than zero", exception.Message);
    }

    [Theory]
    [InlineData(1000000000)] // Very high price
    [InlineData(9999999999)] // Extremely high price
    public void Price_WhenExtremelyHigh_ShouldThrowArgumentException(decimal extremePrice)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Price(extremePrice));
        Assert.Contains("Price exceeds maximum allowed value", exception.Message);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1000)]
    [InlineData(50000)]
    [InlineData(999999.99)]
    public void Price_WhenValidRange_ShouldCreateSuccessfully(decimal validPrice)
    {
        // Act
        var price = new Price(validPrice);

        // Assert
        Assert.Equal(validPrice, price.Value);
    }

    [Fact]
    public void Price_WhenRounding_ShouldRoundToTwoDecimals()
    {
        // Arrange
        var priceWithManyDecimals = 1234.5678m;

        // Act
        var price = new Price(priceWithManyDecimals);

        // Assert
        Assert.Equal(1234.57m, price.Value);
    }

    #endregion

    #region LicensePlate Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LicensePlate_WhenNullOrWhitespace_ShouldThrowArgumentException(string invalidPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidPlate));
        Assert.Contains("License plate cannot be null or whitespace", exception.Message);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOP")] // Too long
    public void LicensePlate_WhenInvalidLength_ShouldThrowArgumentException(string invalidLengthPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidLengthPlate));
        Assert.Contains("License plate must be between 3 and 15 characters", exception.Message);
    }

    [Theory]
    [InlineData("AB@-123")]
    [InlineData("AB#-123")]
    [InlineData("AB$-123")]
    [InlineData("123-@#$")]
    public void LicensePlate_WhenInvalidCharacters_ShouldThrowArgumentException(string invalidCharPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidCharPlate));
        Assert.Contains("License plate contains invalid characters", exception.Message);
    }

    [Theory]
    [InlineData("ABC-123")]
    [InlineData("AB-1234")]
    [InlineData("ABCD-12")]
    [InlineData("A1B-2C3")]
    [InlineData("123-ABC")]
    public void LicensePlate_WhenValidFormat_ShouldCreateSuccessfully(string validPlate)
    {
        // Act
        var licensePlate = new LicensePlate(validPlate);

        // Assert
        Assert.Equal(validPlate.ToUpperInvariant(), licensePlate.Value);
    }

    [Fact]
    public void LicensePlate_WhenLowercase_ShouldConvertToUppercase()
    {
        // Arrange
        var lowercasePlate = "abc-123";

        // Act
        var licensePlate = new LicensePlate(lowercasePlate);

        // Assert
        Assert.Equal("ABC-123", licensePlate.Value);
    }

    [Fact]
    public void LicensePlate_WhenMixedCase_ShouldConvertToUppercase()
    {
        // Arrange
        var mixedCasePlate = "AbC-123";

        // Act
        var licensePlate = new LicensePlate(mixedCasePlate);

        // Assert
        Assert.Equal("ABC-123", licensePlate.Value);
    }

    #endregion

    #region PdfCertification Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public void PdfCertification_WhenNullOrEmpty_ShouldThrowArgumentException(byte[] invalidData)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new PdfCertification(invalidData));
        Assert.Contains("PDF data cannot be null or empty", exception.Message);
    }

    [Fact]
    public void PdfCertification_WhenTooLarge_ShouldThrowArgumentException()
    {
        // Arrange
        var oversizedData = new byte[11 * 1024 * 1024]; // 11MB

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new PdfCertification(oversizedData));
        Assert.Contains("PDF size exceeds maximum allowed", exception.Message);
    }

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })] // %PDF header
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 })] // %PDF-1.4
    public void PdfCertification_WhenValidPdfHeader_ShouldCreateSuccessfully(byte[] validPdfData)
    {
        // Act
        var pdfCertification = new PdfCertification(validPdfData);

        // Assert
        Assert.Equal(validPdfData, pdfCertification.Data);
        Assert.True(pdfCertification.IsValidPdf);
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 })] // Invalid header
    [InlineData(new byte[] { 0x50, 0x44, 0x46 })] // Incomplete header
    public void PdfCertification_WhenInvalidPdfHeader_ShouldThrowArgumentException(byte[] invalidPdfData)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new PdfCertification(invalidPdfData));
        Assert.Contains("Invalid PDF format", exception.Message);
    }

    [Fact]
    public void PdfCertification_WhenValidSize_ShouldCalculateCorrectSize()
    {
        // Arrange
        var testData = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4

        // Act
        var pdfCertification = new PdfCertification(testData);

        // Assert
        Assert.Equal(testData.Length, pdfCertification.SizeInBytes);
    }

    #endregion

    #region Cross-ValueObject Validation Tests

    [Fact]
    public void ValueObjects_WhenMultipleInvalidValues_ShouldThrowAggregateException()
    {
        // Arrange
        var validationErrors = new List<Exception>();

        try { new Year(1800); } catch (Exception ex) { validationErrors.Add(ex); }
        try { new Price(-100); } catch (Exception ex) { validationErrors.Add(ex); }
        try { new LicensePlate(""); } catch (Exception ex) { validationErrors.Add(ex); }

        // Assert
        Assert.Equal(3, validationErrors.Count);
        Assert.All(validationErrors, ex => Assert.IsType<ArgumentException>(ex));
    }

    [Fact]
    public void ValueObjects_WhenAllValid_ShouldCreateSuccessfully()
    {
        // Act
        var year = new Year(2020);
        var price = new Price(25000);
        var licensePlate = new LicensePlate("ABC-123");
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var pdfCertification = new PdfCertification(pdfData);

        // Assert
        Assert.NotNull(year);
        Assert.NotNull(price);
        Assert.NotNull(licensePlate);
        Assert.NotNull(pdfCertification);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void Year_BoundaryValues_ShouldBehaveCorrectly()
    {
        // Test minimum valid year
        var minYear = new Year(1886);
        Assert.Equal(1886, minYear.Value);

        // Test maximum valid year (current year)
        var maxYear = new Year(DateTime.Now.Year);
        Assert.Equal(DateTime.Now.Year, maxYear.Value);

        // Test boundary failures
        Assert.Throws<ArgumentException>(() => new Year(1885));
        Assert.Throws<ArgumentException>(() => new Year(DateTime.Now.Year + 1));
    }

    [Fact]
    public void Price_BoundaryValues_ShouldBehaveCorrectly()
    {
        // Test minimum valid price
        var minPrice = new Price(0.01m);
        Assert.Equal(0.01m, minPrice.Value);

        // Test maximum valid price
        var maxPrice = new Price(999999.99m);
        Assert.Equal(999999.99m, maxPrice.Value);

        // Test boundary failures
        Assert.Throws<ArgumentException>(() => new Price(0m));
        Assert.Throws<ArgumentException>(() => new Price(1000000m));
    }

    [Fact]
    public void LicensePlate_BoundaryLengths_ShouldBehaveCorrectly()
    {
        // Test minimum valid length
        var minLength = new LicensePlate("ABC");
        Assert.Equal("ABC", minLength.Value);

        // Test maximum valid length
        var maxLength = new LicensePlate("ABCDEFGHIJKLMNO"); // 15 characters
        Assert.Equal("ABCDEFGHIJKLMNO", maxLength.Value);

        // Test boundary failures
        Assert.Throws<ArgumentException>(() => new LicensePlate("AB"));
        Assert.Throws<ArgumentException>(() => new LicensePlate("ABCDEFGHIJKLMNOP")); // 16 characters
    }

    #endregion
}
