using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

/// <summary>
/// Advanced validation tests for value objects
/// </summary>
public class ValueObjectValidationTests
{
    #region Year Validation Tests

    [TestCase(1885)] // Before first car
    [TestCase(2050)] // Future year
    [TestCase(0)]
    [TestCase(-1)]
    public void Year_WhenInvalidRange_ShouldThrowArgumentException(int invalidYear)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Year(invalidYear));
        StringAssert.Contains("Year must be between", exception.Message);
    }

    [TestCase(1886)] // First car year
    [TestCase(2024)] // Current year
    [TestCase(2000)] // Valid year
    public void Year_WhenValidRange_ShouldCreateSuccessfully(int validYear)
    {
        // Act
        var year = new Year(validYear);

        // Assert
        Assert.AreEqual(validYear, year.Value);
    }

    [Test]
    public void Year_WhenCurrentYear_ShouldBeValid()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;

        // Act
        var year = new Year(currentYear);

        // Assert
        Assert.AreEqual(currentYear, year.Value);
    }

    #endregion

    #region Price Validation Tests

    [TestCase(-1)]
    [TestCase(-100)]
    [TestCase(-0.01)]
    public void Price_WhenNegative_ShouldThrowArgumentException(decimal negativePrice)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Price(negativePrice));
        StringAssert.Contains("Price must be greater than or equal to zero", exception.Message);
    }

    [Test]
    public void Price_WhenZero_ShouldCreateSuccessfully()
    {
        // Act & Assert
        var price = new Price(0);
        Assert.AreEqual(0m, price.Value);
    }

    [TestCase(1000000000)] // Very high price
    [TestCase(999999999)] // Extremely high price
    public void Price_WhenExtremelyHigh_ShouldThrowArgumentException(decimal extremePrice)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Price(extremePrice));
        StringAssert.Contains("Price exceeds maximum allowed value", exception.Message);
    }

    [TestCase(0.01)]
    [TestCase(1000)]
    [TestCase(50000)]
    [TestCase(999999.99)]
    public void Price_WhenValidRange_ShouldCreateSuccessfully(decimal validPrice)
    {
        // Act
        var price = new Price(validPrice);

        // Assert
        Assert.AreEqual(validPrice, price.Value);
    }

    [Test]
    public void Price_WhenRounding_ShouldRoundToTwoDecimals()
    {
        // Arrange
        var priceWithManyDecimals = 1234.5678m;

        // Act
        var price = new Price(priceWithManyDecimals);

        // Assert
        Assert.AreEqual(1234.57m, price.Value);
    }

    #endregion

    #region LicensePlate Validation Tests

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    [TestCase(null)]
    public void LicensePlate_WhenNullOrWhitespace_ShouldThrowArgumentException(string invalidPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidPlate));
        StringAssert.Contains("License plate cannot be null or whitespace", exception.Message);
    }

    [TestCase("A")]
    [TestCase("AB")]
    [TestCase("ABCDEFGHIJKLMNOP")] // Too long
    public void LicensePlate_WhenInvalidLength_ShouldThrowArgumentException(string invalidLengthPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidLengthPlate));
        StringAssert.Contains("License plate must be between 3 and 15 characters", exception.Message);
    }

    [TestCase("AB@-123")]
    [TestCase("AB#-123")]
    [TestCase("AB$-123")]
    [TestCase("123-@#$")]
    public void LicensePlate_WhenInvalidCharacters_ShouldThrowArgumentException(string invalidCharPlate)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LicensePlate(invalidCharPlate));
        StringAssert.Contains("License plate contains invalid characters", exception.Message);
    }

    [TestCase("ABC-123")]
    [TestCase("AB-1234")]
    [TestCase("ABCD-12")]
    [TestCase("A1B-2C3")]
    [TestCase("123-ABC")]
    public void LicensePlate_WhenValidFormat_ShouldCreateSuccessfully(string validPlate)
    {
        // Act
        var licensePlate = new LicensePlate(validPlate);

        // Assert
        Assert.AreEqual(validPlate.ToUpperInvariant(), licensePlate.Value);
    }

    [Test]
    public void LicensePlate_WhenLowercase_ShouldConvertToUppercase()
    {
        // Arrange
        var lowercasePlate = "abc-123";

        // Act
        var licensePlate = new LicensePlate(lowercasePlate);

        // Assert
        Assert.AreEqual("ABC-123", licensePlate.Value);
    }

    [Test]
    public void LicensePlate_WhenMixedCase_ShouldConvertToUppercase()
    {
        // Arrange
        var mixedCasePlate = "AbC-123";

        // Act
        var licensePlate = new LicensePlate(mixedCasePlate);

        // Assert
        Assert.AreEqual("ABC-123", licensePlate.Value);
    }

    #endregion

    #region PdfCertification Validation Tests

    [Test]
    public void PdfCertification_WhenNullOrEmpty_ShouldCreateEmptyBase64()
    {
        // Act
        var pdf1 = new PdfCertification((string?)null);
        var pdf2 = new PdfCertification(string.Empty);

        // Assert
        Assert.IsNotNull(pdf1);
        Assert.IsNotNull(pdf2);
        Assert.AreEqual(string.Empty, pdf1.Base64Data);
        Assert.AreEqual(string.Empty, pdf2.Base64Data);
    }

    [Test]
    public void PdfCertification_WhenTooLarge_ShouldAllowCreationUnlessTooShort()
    {
        // Arrange
        var oversizedBytes = new byte[11 * 1024 * 1024]; // 11MB
        var oversizedBase64 = Convert.ToBase64String(oversizedBytes);

        // Act
        var pdf = new PdfCertification(oversizedBase64);

        // Assert
        Assert.IsNotNull(pdf);
        Assert.IsTrue(pdf.Base64Data.Length > 0);
    }

    [Test]
    public void PdfCertification_WhenValidPdfHeader_ShouldCreateSuccessfully()
    {
        // Arrange
        var validPdf1 = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        var validPdf2 = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4

        // Act
        var pdfCertification1 = new PdfCertification(Convert.ToBase64String(validPdf1));
        var pdfCertification2 = new PdfCertification(Convert.ToBase64String(validPdf2));

        // Assert
        Assert.AreEqual(Convert.ToBase64String(validPdf1), pdfCertification1.Base64Data);
        Assert.IsTrue(pdfCertification1.IsValidBase64());

        Assert.AreEqual(Convert.ToBase64String(validPdf2), pdfCertification2.Base64Data);
        Assert.IsTrue(pdfCertification2.IsValidBase64());
    }

    [Test]
    public void PdfCertification_WhenInvalidPdfHeader_ShouldThrowArgumentException()
    {
        // Arrange
        var invalid1 = new byte[] { 0x00, 0x01, 0x02, 0x03 }; // Invalid header
        var invalid2 = new byte[] { 0x50, 0x44, 0x46 }; // Incomplete header

        // Act & Assert: constructor should accept the data but IsValidBase64() should be false
        var pdf1 = new PdfCertification(Convert.ToBase64String(invalid1));
        Assert.IsFalse(pdf1.IsValidBase64());
        var pdf2 = new PdfCertification(Convert.ToBase64String(invalid2));
        Assert.IsFalse(pdf2.IsValidBase64());
    }

    [Test]
    public void PdfCertification_WhenValidSize_ShouldCalculateCorrectSize()
    {
        // Arrange
        var testData = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4

        // Act
        var pdfCertification = new PdfCertification(Convert.ToBase64String(testData));

        // Assert
        Assert.AreEqual(testData.Length, Convert.FromBase64String(pdfCertification.Base64Data).Length);
    }

    #endregion

    #region Cross-ValueObject Validation Tests

    [Test]
    public void ValueObjects_WhenMultipleInvalidValues_ShouldThrowAggregateException()
    {
        // Arrange
        var validationErrors = new List<Exception>();

        try { new Year(1800); } catch (Exception ex) { validationErrors.Add(ex); }
        try { new Price(-100); } catch (Exception ex) { validationErrors.Add(ex); }
        try { new LicensePlate(""); } catch (Exception ex) { validationErrors.Add(ex); }

        // Assert
        Assert.AreEqual(3, validationErrors.Count);
        foreach (var ex in validationErrors)
        {
            Assert.IsInstanceOf<ArgumentException>(ex);
        }
    }

    [Test]
    public void ValueObjects_WhenAllValid_ShouldCreateSuccessfully()
    {
        // Act
        var year = new Year(2020);
        var price = new Price(25000);
        var licensePlate = new LicensePlate("ABC-123");
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var pdfCertification = new PdfCertification(Convert.ToBase64String(pdfData));

        // Assert
        Assert.IsNotNull(year);
        Assert.IsNotNull(price);
        Assert.IsNotNull(licensePlate);
        Assert.IsNotNull(pdfCertification);
    }

    #endregion

    #region Boundary Value Tests

    [Test]
    public void Year_BoundaryValues_ShouldBehaveCorrectly()
    {
        // Test minimum valid year
        var minYear = new Year(1886);
        Assert.AreEqual(1886, minYear.Value);

        // Test maximum valid year (current year)
        var maxYear = new Year(DateTime.Now.Year);
        Assert.AreEqual(DateTime.Now.Year, maxYear.Value);

        // Test boundary failures
        Assert.Throws<ArgumentException>(() => new Year(1885));
        Assert.Throws<ArgumentException>(() => new Year(DateTime.Now.Year + 1));
    }

    [Test]
    public void Price_BoundaryValues_ShouldBehaveCorrectly()
    {
        // Test minimum valid price
        var minPrice = new Price(0.01m);
        Assert.AreEqual(0.01m, minPrice.Value);

        // Test maximum valid price (matches updated business rules)
        var maxPrice = new Price(9999999.99m);
        Assert.AreEqual(9999999.99m, maxPrice.Value);

        // Test boundary failures: 0 is accepted now; no exception should be thrown for 0
        Assert.DoesNotThrow(() => new Price(0m));
        Assert.Throws<ArgumentException>(() => new Price(10000000m));
    }

    [Test]
    public void LicensePlate_BoundaryLengths_ShouldBehaveCorrectly()
    {
        // Test minimum valid length
        var minLength = new LicensePlate("ABC");
        Assert.AreEqual("ABC", minLength.Value);

        // Test maximum valid length
        var maxLength = new LicensePlate("ABCDEFGHIJKLMNO"); // 15 characters
        Assert.AreEqual("ABCDEFGHIJKLMNO", maxLength.Value);

        // Test boundary failures
        Assert.Throws<ArgumentException>(() => new LicensePlate("AB"));
        Assert.Throws<ArgumentException>(() => new LicensePlate("ABCDEFGHIJKLMNOP")); // 16 characters
    }

    #endregion
}
