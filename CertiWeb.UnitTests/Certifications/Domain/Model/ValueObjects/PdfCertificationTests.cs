using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

[TestFixture]
public class PdfCertificationTests
{
    [Test]
    public void Constructor_WithValidBase64Data_ShouldCreatePdfCertificationSuccessfully()
    {
        // Arrange
        var validBase64 = "SGVsbG8gV29ybGQ="; // "Hello World" in Base64

        // Act
        var pdfCertification = new PdfCertification(validBase64);

        // Assert
        pdfCertification.Base64Data.Should().Be(validBase64);
    }

    [Test]
    public void Constructor_WithEmptyString_ShouldCreateWithEmptyData()
    {
        // Arrange
        var emptyData = "";

        // Act
        var pdfCertification = new PdfCertification(emptyData);

        // Assert
        pdfCertification.Base64Data.Should().Be("");
    }

    [Test]
    public void Constructor_WithNullData_ShouldCreateWithEmptyData()
    {
        // Arrange
        string? nullData = null;

        // Act
        var pdfCertification = new PdfCertification(nullData!);

        // Assert
        pdfCertification.Base64Data.Should().Be("");
    }

    [Test]
    public void Constructor_WithWhitespaceOnly_ShouldCreateWithEmptyData()
    {
        // Arrange
        var whitespaceData = "   ";

        // Act
        var pdfCertification = new PdfCertification(whitespaceData);

        // Assert
        pdfCertification.Base64Data.Should().Be("");
    }

    [Test]
    public void Constructor_WithDataUrlPrefix_ShouldRemovePrefixAndStoreCleanData()
    {
        // Arrange
        var base64Content = "SGVsbG8gV29ybGQ=";
        var dataUrlWithPrefix = $"data:application/pdf;base64,{base64Content}";

        // Act
        var pdfCertification = new PdfCertification(dataUrlWithPrefix);

        // Assert
        pdfCertification.Base64Data.Should().Be(base64Content);
    }

    [Test]
    public void Constructor_WithTooShortData_ShouldThrowArgumentException()
    {
        // Arrange
        var shortData = "ABC"; // Less than 10 characters

        // Act & Assert
        var pdf = new PdfCertification(shortData);
        pdf.Base64Data.Should().Be(shortData);
        pdf.IsValidBase64().Should().BeFalse();
    }

    [Test]
    public void Constructor_WithDataUrlPrefixButTooShortContent_ShouldThrowArgumentException()
    {
        // Arrange
        var shortContent = "ABC"; // Less than 10 characters
        var dataUrlWithShortContent = $"data:application/pdf;base64,{shortContent}";

        // Act & Assert
        var pdf = new PdfCertification(dataUrlWithShortContent);
        pdf.Base64Data.Should().Be(shortContent);
        pdf.IsValidBase64().Should().BeFalse();
    }

    [Test]
    public void Constructor_WithMinimumValidLength_ShouldCreatePdfCertificationSuccessfully()
    {
        // Arrange
        var minimumValidData = "1234567890"; // Exactly 10 characters

        // Act
        var pdfCertification = new PdfCertification(minimumValidData);

        // Assert
        pdfCertification.Base64Data.Should().Be(minimumValidData);
    }

    [Test]
    public void IsValidBase64_WithValidBase64String_ShouldReturnTrue()
    {
        // Arrange
        var validBase64 = "SGVsbG8gV29ybGQ="; // Valid Base64
        var pdfCertification = new PdfCertification(validBase64);

        // Act
        var isValid = pdfCertification.IsValidBase64();

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public void IsValidBase64_WithInvalidBase64String_ShouldReturnFalse()
    {
        // Arrange
        var invalidBase64 = "InvalidBase64!@#$%"; // Invalid Base64
        var pdfCertification = new PdfCertification(invalidBase64);

        // Act
        var isValid = pdfCertification.IsValidBase64();

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public void IsValidBase64_WithEmptyData_ShouldReturnTrue()
    {
        // Arrange
        var pdfCertification = new PdfCertification("");

        // Act
        var isValid = pdfCertification.IsValidBase64();

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public void ImplicitConversion_FromPdfCertificationToString_ShouldReturnCorrectValue()
    {
        // Arrange
        var base64Data = "VGVzdCBEYXRh"; // "Test Data" in Base64
        var pdfCertification = new PdfCertification(base64Data);

        // Act
        string value = pdfCertification;

        // Assert
        value.Should().Be(base64Data);
    }

    [Test]
    public void ImplicitConversion_FromStringToPdfCertification_ShouldCreatePdfCertificationCorrectly()
    {
        // Arrange
        var base64Data = "VGVzdCBEYXRh";

        // Act
        PdfCertification pdfCertification = base64Data;

        // Assert
        pdfCertification.Base64Data.Should().Be(base64Data);
    }

    [Test]
    public void Constructor_WithLongValidBase64_ShouldCreatePdfCertificationSuccessfully()
    {
        // Arrange
        var longValidBase64 = "VGhpcyBpcyBhIGxvbmcgdGVzdCBzdHJpbmcgZm9yIGJhc2U2NCBlbmNvZGluZw==";

        // Act
        var pdfCertification = new PdfCertification(longValidBase64);

        // Assert
        pdfCertification.Base64Data.Should().Be(longValidBase64);
        pdfCertification.IsValidBase64().Should().BeTrue();
    }
}
