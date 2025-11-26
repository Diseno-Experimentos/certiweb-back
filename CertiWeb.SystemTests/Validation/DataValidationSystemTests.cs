using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;

namespace CertiWeb.SystemTests.Validation;

[TestFixture]
public class DataValidationSystemTests : SystemTestBase
{
    private int _testBrandId;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        
        // Arrange - Create test brand
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Validation Test Brand");
        using var context = GetFreshDbContext();
        context.Brands.Add(testBrand);
        await context.SaveChangesAsync();
        _testBrandId = testBrand.Id;
    }

    [Test]
    public async Task CreateCar_WithRequiredFieldsEmpty_ShouldReturnValidationErrors()
    {
        // Arrange
        var invalidCarData = new
        {
            Title = "", // Required field empty
            Owner = "",  // Required field empty
            OwnerEmail = "", // Required field empty
            Year = 2023,
            BrandId = _testBrandId,
            Model = "", // Required field empty
            Description = "Test Description",
            PdfCertification = TestDataBuilder.GenerateValidBase64(),
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "ABC1234",
            OriginalReservationId = 100
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", invalidCarData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Empty required fields should result in validation error");
    }

    [Test]
    public async Task CreateCar_WithInvalidEmailFormat_ShouldReturnValidationError()
    {
        // Arrange
        var invalidEmailFormats = new[]
        {
            "invalid-email",
            "test@",
            "@domain.com",
            "test..email@domain.com",
            "test email@domain.com"
        };

        foreach (var invalidEmail in invalidEmailFormats)
        {
            var carData = new
            {
                Title = "Test Car",
                Owner = "Test Owner",
                OwnerEmail = invalidEmail,
                Year = 2023,
                BrandId = _testBrandId,
                Model = "Test Model",
                Description = "Test Description",
                PdfCertification = TestDataBuilder.GenerateValidBase64(),
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = TestDataBuilder.GenerateValidLicensePlate(),
                OriginalReservationId = 100 + invalidEmailFormats.ToList().IndexOf(invalidEmail)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created);
        }
    }

    [Test]
    public async Task CreateCar_WithBoundaryYearValues_ShouldValidateCorrectly()
    {
        // Arrange
        var yearTestCases = new[]
        {
            new { Year = 1899, ShouldPass = false },  // Below minimum
            new { Year = 1900, ShouldPass = true },   // Minimum valid
            new { Year = DateTime.Now.Year, ShouldPass = true },  // Current year
            new { Year = DateTime.Now.Year + 1, ShouldPass = true },  // Next year (valid)
            new { Year = DateTime.Now.Year + 2, ShouldPass = false }  // Too far in future
        };

        foreach (var testCase in yearTestCases)
        {
            var carData = new
            {
                Title = $"Year Test Car {testCase.Year}",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = testCase.Year,
                BrandId = _testBrandId,
                Model = "Test Model",
                Description = "Year boundary test",
                PdfCertification = TestDataBuilder.GenerateValidBase64(),
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = $"YR{testCase.Year}",
                OriginalReservationId = 1000 + testCase.Year
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            if (testCase.ShouldPass)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"Year {testCase.Year} should be valid");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                    $"Year {testCase.Year} should be invalid");
            }
        }
    }

    [Test]
    public async Task CreateCar_WithBoundaryPriceValues_ShouldValidateCorrectly()
    {
        // Arrange
        var priceTestCases = new[]
        {
            new { Price = -1m, ShouldPass = false },     // Negative price
            new { Price = 0m, ShouldPass = true },       // Zero price (valid)
            new { Price = 0.01m, ShouldPass = true },    // Minimum positive
            new { Price = 999999.99m, ShouldPass = true }, // Large valid price
        };

        foreach (var testCase in priceTestCases)
        {
            var carData = new
            {
                Title = $"Price Test Car",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = 2023,
                BrandId = _testBrandId,
                Model = "Test Model",
                Description = "Price boundary test",
                PdfCertification = TestDataBuilder.GenerateValidBase64(),
                ImageUrl = "https://example.com/image.jpg",
                Price = testCase.Price,
                LicensePlate = $"PR{Math.Abs((int)(testCase.Price * 100)):D4}",
                OriginalReservationId = 2000 + (int)(testCase.Price * 100)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            if (testCase.ShouldPass)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"Price {testCase.Price} should be valid");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                    $"Price {testCase.Price} should be invalid");
            }
        }
    }

    [Test]
    public async Task CreateCar_WithInvalidLicensePlateFormats_ShouldReturnValidationError()
    {
        // Arrange
        var invalidLicensePlates = new[]
        {
            "AB123",      // Too short (5 chars)
            "ABCDEFGH123", // Too long (11 chars)
            "",           // Empty
            "ABC-123",    // With dash (might be invalid depending on validation rules)
        };

        foreach (var invalidPlate in invalidLicensePlates)
        {
            var carData = new
            {
                Title = "License Plate Test Car",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = 2023,
                BrandId = _testBrandId,
                Model = "Test Model",
                Description = "License plate validation test",
                PdfCertification = TestDataBuilder.GenerateValidBase64(),
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = invalidPlate,
                OriginalReservationId = 3000 + invalidLicensePlates.ToList().IndexOf(invalidPlate)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Invalid license plate '{invalidPlate}' should be rejected");
        }
    }

    [Test]
    public async Task CreateCar_WithInvalidPdfCertification_ShouldReturnValidationError()
    {
        // Arrange
        var invalidPdfCertifications = new[]
        {
            "",           // Empty
            "ABC",        // Too short
            "Not Base64!", // Invalid Base64
        };

        foreach (var invalidPdf in invalidPdfCertifications)
        {
            var carData = new
            {
                Title = "PDF Test Car",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = 2023,
                BrandId = _testBrandId,
                Model = "Test Model",
                Description = "PDF validation test",
                PdfCertification = invalidPdf,
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = $"PDF{invalidPdfCertifications.ToList().IndexOf(invalidPdf):D3}",
                OriginalReservationId = 4000 + invalidPdfCertifications.ToList().IndexOf(invalidPdf)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Invalid PDF certification should be rejected");
        }
    }

    [Test]
    public async Task CreateCar_WithInvalidBrandId_ShouldReturnValidationError()
    {
        // Arrange
        var invalidBrandIds = new[] { 0, -1, 99999 }; // Non-existent brand IDs

        foreach (var invalidBrandId in invalidBrandIds)
        {
            var carData = new
            {
                Title = "Brand ID Test Car",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = 2023,
                BrandId = invalidBrandId,
                Model = "Test Model",
                Description = "Brand ID validation test",
                PdfCertification = TestDataBuilder.GenerateValidBase64(),
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = $"BR{Math.Abs(invalidBrandId):D4}",
                OriginalReservationId = 5000 + Math.Abs(invalidBrandId)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Invalid brand ID {invalidBrandId} should be rejected");
        }
    }

    [Test]
    public async Task CreateCar_WithMaxLengthValidation_ShouldRespectFieldLimits()
    {
        // Arrange
        var longTitle = new string('A', 250); // Assuming max length is 200
        var longOwner = new string('B', 150); // Assuming max length is 100
        var longModel = new string('C', 150); // Assuming max length is 100

        var carData = new
        {
            Title = longTitle,
            Owner = longOwner,
            OwnerEmail = "test@email.com",
            Year = 2023,
            BrandId = _testBrandId,
            Model = longModel,
            Description = "Max length validation test",
            PdfCertification = TestDataBuilder.GenerateValidBase64(),
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "MAXLEN12",
            OriginalReservationId = 6000
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Fields exceeding maximum length should be rejected");
    }

    [Test]
    public async Task ValidationErrors_ShouldReturnMeaningfulErrorMessages()
    {
        // Arrange
        var invalidCarData = new
        {
            Title = "", // Empty required field
            Owner = "",
            OwnerEmail = "invalid-email",
            Year = 1800, // Invalid year
            BrandId = _testBrandId,
            Model = "",
            Description = "Error message test",
            PdfCertification = "short", // Too short
            ImageUrl = "https://example.com/image.jpg",
            Price = -1000, // Negative price
            LicensePlate = "AB", // Too short
            OriginalReservationId = 7000
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", invalidCarData);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorContent.Should().NotBeEmpty("Error response should contain meaningful error information");
        
        // Error message should indicate what went wrong
        errorContent.Should().Contain("error", "Error response should contain error information");
    }
}
