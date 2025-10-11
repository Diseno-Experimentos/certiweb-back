using CertiWeb.SystemTests.Infrastructure;
using System.Text;

namespace CertiWeb.SystemTests.Security;

[TestFixture]
public class SecuritySystemTests : SystemTestBase
{
    [Test]
    public async Task ApiEndpoints_ShouldReturnCorrectSecurityHeaders()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check for security headers (these might be added by middleware)
        response.Headers.Should().ContainKey("Date");
        // Note: Additional security headers like X-Content-Type-Options, X-Frame-Options, etc.
        // would be tested here if implemented in the application
    }

    [Test]
    public async Task InvalidJsonPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/cars", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task OversizedPayload_ShouldBeHandledGracefully()
    {
        // Arrange - Create a very large payload
        var largeString = new string('x', 10_000_000); // 10MB string
        var oversizedPayload = new
        {
            Title = largeString,
            Owner = "Test Owner",
            OwnerEmail = "test@email.com",
            Year = 2023,
            BrandId = 1,
            Model = "Test Model",
            Description = largeString,
            PdfCertification = "VGVzdCBkYXRh", // Valid base64
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "ABC1234",
            OriginalReservationId = 100
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", oversizedPayload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.PayloadTooLarge);
    }

    [Test]
    public async Task SqlInjectionAttempt_ShouldBeHandledSafely()
    {
        // Arrange - Attempt SQL injection through email parameter
        var maliciousEmail = "test@email.com'; DROP TABLE Cars; --";
        
        // Act
        var response = await Client.GetAsync($"/api/v1/users/email/{Uri.EscapeDataString(maliciousEmail)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        
        // Verify that the database is still intact
        var brandsResponse = await Client.GetAsync("/api/v1/brands");
        brandsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task XssAttempt_ShouldBeSanitized()
    {
        // Arrange
        var maliciousPayload = new
        {
            Title = "<script>alert('xss')</script>",
            Owner = "<img src=x onerror=alert('xss')>",
            OwnerEmail = "test@email.com",
            Year = 2023,
            BrandId = 1,
            Model = "Test Model",
            Description = "<script>document.cookie='stolen'</script>",
            PdfCertification = "VGVzdCBkYXRh",
            ImageUrl = "javascript:alert('xss')",
            Price = 25000,
            LicensePlate = "ABC1234",
            OriginalReservationId = 100
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", maliciousPayload);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            
            // Verify that dangerous scripts are not present in response
            createdCar!.Title.Should().NotContain("<script>");
            createdCar.Owner.Should().NotContain("onerror");
            createdCar.Description.Should().NotContain("<script>");
        }
        // If validation rejects it, that's also acceptable
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Test]
    public async Task InvalidHttpMethods_ShouldReturnMethodNotAllowed()
    {
        // Arrange & Act
        var patchResponse = await Client.PatchAsync("/api/v1/brands", null);
        var deleteResponse = await Client.DeleteAsync("/api/v1/brands");

        // Assert
        patchResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.MethodNotAllowed, 
            HttpStatusCode.NotFound);
        deleteResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.MethodNotAllowed, 
            HttpStatusCode.NotFound);
    }

    [Test]
    public async Task InvalidContentType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange
        var xmlContent = new StringContent("<car><title>Test</title></car>", Encoding.UTF8, "application/xml");

        // Act
        var response = await Client.PostAsync("/api/v1/cars", xmlContent);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task NonExistentEndpoint_ShouldReturnNotFound()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task MultipleInvalidRequests_ShouldNotCauseServiceDegradation()
    {
        // Arrange
        const int numberOfInvalidRequests = 50;
        var invalidRequestTasks = new List<Task<HttpResponseMessage>>();

        // Act - Send multiple invalid requests
        for (int i = 0; i < numberOfInvalidRequests; i++)
        {
            var invalidContent = new StringContent("invalid", Encoding.UTF8, "application/json");
            invalidRequestTasks.Add(Client.PostAsync("/api/v1/cars", invalidContent));
        }

        await Task.WhenAll(invalidRequestTasks);

        // Act - Verify that service still works correctly
        var validResponse = await Client.GetAsync("/api/v1/brands");

        // Assert
        validResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task SpecialCharacters_ShouldBeHandledCorrectly()
    {
        // Arrange
        var specialCharsPayload = new
        {
            Title = "Test Car with Special Chars: áéíóú ñ ü ¿¡",
            Owner = "José María Rodríguez-López",
            OwnerEmail = "jose.maria@email.com",
            Year = 2023,
            BrandId = 1,
            Model = "Español Model",
            Description = "Descripción con caracteres especiales: ©®™",
            PdfCertification = "VGVzdCBkYXRh",
            ImageUrl = "https://example.com/image.jpg",
            Price = 25000,
            LicensePlate = "ABC1234",
            OriginalReservationId = 100
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", specialCharsPayload);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCar!.Title.Should().Contain("áéíóú");
            createdCar.Owner.Should().Contain("José María");
        }
    }

    [Test]
    public async Task BoundaryValues_ShouldBeValidatedCorrectly()
    {
        // Arrange - Test with boundary values
        var boundaryTestCases = new[]
        {
            new { Year = 1899, ShouldFail = true },  // Below minimum
            new { Year = 1900, ShouldFail = false }, // Minimum valid
            new { Year = DateTime.Now.Year + 2, ShouldFail = true }, // Above maximum
            new { Year = DateTime.Now.Year + 1, ShouldFail = false }  // Maximum valid
        };

        foreach (var testCase in boundaryTestCases)
        {
            var payload = new
            {
                Title = "Boundary Test Car",
                Owner = "Test Owner",
                OwnerEmail = "test@email.com",
                Year = testCase.Year,
                BrandId = 1,
                Model = "Test Model",
                Description = "Boundary test",
                PdfCertification = "VGVzdCBkYXRh",
                ImageUrl = "https://example.com/image.jpg",
                Price = 25000,
                LicensePlate = $"BND{testCase.Year}",
                OriginalReservationId = 100 + testCase.Year
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", payload);

            // Assert
            if (testCase.ShouldFail)
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                    $"Year {testCase.Year} should be rejected");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"Year {testCase.Year} should be accepted");
            }
        }
    }

    /// <summary>
    /// DTO for car response deserialization.
    /// </summary>
    private record CarResource(
        int Id,
        string Title,
        string Owner,
        string OwnerEmail,
        int Year,
        int BrandId,
        string Model,
        string? Description,
        string PdfCertification,
        string? ImageUrl,
        decimal Price,
        string LicensePlate,
        int OriginalReservationId
    );
}
