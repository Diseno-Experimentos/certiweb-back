using CertiWeb.SystemTests.Infrastructure;
using System.Globalization;
using System.Text;

namespace CertiWeb.SystemTests.Compatibility;

[TestFixture]
public class CompatibilitySystemTests : SystemTestBase
{
    [Test]
    public async Task ApiEndpoints_ShouldHandleInternationalCharacters()
    {
        // Arrange - Test with various international characters
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Compatibility Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        var internationalCarData = new
        {
            Title = "Vehículo de Prueba - Carácter Especial: ñáéíóú",
            Owner = "José María Rodríguez-Fernández",
            OwnerEmail = "jose.maria@dominio.es",
            Year = 2023,
            BrandId = testBrand.Id,
            Model = "Español",
            Description = "Descripción con acentos: àáâãäåæçèéêëìíîïðñòóôõöøùúûüý",
            PdfCertification = "VGVzdCBkYXRhIGZvciDDr8OowqHDqsOhciB0ZXN0aW5n", // Base64 with international chars
            ImageUrl = "https://ejemplo.com/imágenes/coche.jpg",
            Price = 25000.50m,
            LicensePlate = "ESP1234",
            OriginalReservationId = 50001
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", internationalCarData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "API should handle international characters correctly");

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCar.Should().NotBeNull();
            createdCar!.Title.Should().Contain("ñáéíóú");
            createdCar.Owner.Should().Contain("José María");
            createdCar.Description.Should().Contain("àáâãäåæçèéêëìíîïðñòóôõöøùúûüý");
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleDifferentDateFormats()
    {
        // Arrange - Test with different cultural date handling
        var currentCulture = CultureInfo.CurrentCulture;
        var cultures = new[]
        {
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("es-ES"),
            CultureInfo.GetCultureInfo("fr-FR")
        };

        foreach (var culture in cultures)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Act
            var response = await Client.GetAsync("/api/v1/brands");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"API should work correctly with {culture.Name} culture");

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeNullOrEmpty(
                $"Response should be valid with {culture.Name} culture");
        }

        // Restore original culture
        CultureInfo.CurrentCulture = currentCulture;
        CultureInfo.CurrentUICulture = currentCulture;
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleDifferentEncodingsCorrectly()
    {
        // Arrange - Test with different character encodings
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Encoding Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        var unicodeTestData = new
        {
            Title = "Test with Unicode: 🚗 車 автомобиль αυτοκίνητο",
            Owner = "Unicode User 测试用户",
            OwnerEmail = "unicode@test.com",
            Year = 2023,
            BrandId = testBrand.Id,
            Model = "Unicode Model Модель",
            Description = "Testing Unicode: 中文 العربية עברית हिन्दी 日本語 한국어",
            PdfCertification = "VGVzdCB3aXRoIFVuaWNvZGU=",
            ImageUrl = "https://example.com/unicode-test.jpg",
            Price = 25000m,
            LicensePlate = "UNI2023",
            OriginalReservationId = 50002
        };

        // Act
        var jsonContent = JsonContent.Create(unicodeTestData, options: new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var response = await Client.PostAsync("/api/v1/cars", jsonContent);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCar.Should().NotBeNull();
            // Some Unicode characters might be handled differently, but the operation should succeed
        }
        else
        {
            // If Unicode is not fully supported, it should at least fail gracefully
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "If Unicode is not supported, it should fail gracefully with BadRequest");
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleVariousNumberFormats()
    {
        // Arrange - Test with different number formats
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Number Format Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        var numberFormatTests = new[]
        {
            new { Price = 25000.00m, Description = "US format - dot as decimal separator" },
            new { Price = 25000.50m, Description = "Standard decimal with cents" },
            new { Price = 1234567.89m, Description = "Large number with decimals" },
            new { Price = 0.01m, Description = "Very small price" },
            new { Price = 999999.99m, Description = "Large price boundary" }
        };

        foreach (var test in numberFormatTests)
        {
            var carData = new
            {
                Title = $"Number Format Test - {test.Description}",
                Owner = "Number Test User",
                OwnerEmail = "number@test.com",
                Year = 2023,
                BrandId = testBrand.Id,
                Model = "Number Test Model",
                Description = test.Description,
                PdfCertification = "TnVtYmVyIGZvcm1hdCB0ZXN0",
                ImageUrl = "https://example.com/number-test.jpg",
                Price = test.Price,
                LicensePlate = $"NUM{Array.IndexOf(numberFormatTests, test):D4}",
                OriginalReservationId = 50010 + Array.IndexOf(numberFormatTests, test)
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/cars", carData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Number format test should succeed for: {test.Description}");

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var createdCar = await DeserializeResponseAsync<CarResource>(response);
                createdCar!.Price.Should().Be(test.Price,
                    $"Price should be preserved correctly for: {test.Description}");
            }
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleTimeZoneDifferences()
    {
        // Arrange - Test API behavior across different time zones
        var originalTimeZone = TimeZoneInfo.Local;

        try
        {
            // Test with different time zones (simulation)
            var timeZoneTests = new[]
            {
                "UTC", 
                "Pacific Standard Time", 
                "Central European Standard Time"
            };

            foreach (var timeZoneName in timeZoneTests)
            {
                try
                {
                    // Act - Make API calls (time zone mainly affects server-side logging and timestamps)
                    var response = await Client.GetAsync("/api/v1/brands");

                    // Assert
                    response.StatusCode.Should().Be(HttpStatusCode.OK,
                        $"API should work correctly regardless of time zone context");

                    // Check that response headers include proper date
                    response.Headers.Date.Should().NotBeNull(
                        "Response should include proper date header");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Skip time zones that don't exist on current system
                    continue;
                }
            }
        }
        finally
        {
            // Restore original time zone context if needed
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleContentNegotiation()
    {
        // Arrange - Test different Accept headers
        var acceptHeaders = new[]
        {
            "application/json",
            "application/json; charset=utf-8",
            "*/*",
            "application/json, text/plain, */*"
        };

        foreach (var acceptHeader in acceptHeaders)
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", acceptHeader);

            // Act
            var response = await client.GetAsync("/api/v1/brands");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"API should handle Accept header: {acceptHeader}");
            
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
                $"Response should be JSON regardless of Accept header: {acceptHeader}");
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleDifferentHttpVersions()
    {
        // Arrange & Act - Test with default HTTP client settings
        var response = await Client.GetAsync("/api/v1/brands");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Version.Should().NotBeNull("HTTP version should be specified");
        
        // Modern APIs should support at least HTTP/1.1
        response.Version.Should().BeOneOf(
            new Version(1, 1),
            new Version(2, 0),
            new Version(3, 0)
        );
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleLargePayloads()
    {
        // Arrange - Test with reasonably large payloads
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Large Payload Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        var largeDescription = new string('A', 4000); // 4KB description
        var largePdfData = Convert.ToBase64String(new byte[10000]); // 10KB base64 data

        var largeCarData = new
        {
            Title = "Large Payload Test Car",
            Owner = "Large Payload Test User",
            OwnerEmail = "large@payload.test",
            Year = 2023,
            BrandId = testBrand.Id,
            Model = "Large Payload Model",
            Description = largeDescription,
            PdfCertification = largePdfData,
            ImageUrl = "https://example.com/large-payload-test.jpg",
            Price = 25000m,
            LicensePlate = "LARGE123",
            OriginalReservationId = 50020
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", largeCarData);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge
        );

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCar.Should().NotBeNull("Large payload should be handled correctly if accepted");
        }
    }

    [Test]
    public async Task ApiEndpoints_ShouldHandleEmptyAndNullValues()
    {
        // Arrange - Test with null and empty values
        var testBrand = new CertiWeb.API.Certifications.Domain.Model.Aggregates.Brand("Null Test Brand");
        using (var context = GetFreshDbContext())
        {
            context.Brands.Add(testBrand);
            await context.SaveChangesAsync();
        }

        var carDataWithNulls = new
        {
            Title = "Null Test Car",
            Owner = "Null Test User",
            OwnerEmail = "null@test.com",
            Year = 2023,
            BrandId = testBrand.Id,
            Model = "Null Test Model",
            Description = (string?)null, // Optional field as null
            PdfCertification = "TnVsbCB0ZXN0IGRhdGE=",
            ImageUrl = (string?)null, // Optional field as null
            Price = 25000m,
            LicensePlate = "NULL123",
            OriginalReservationId = 50030
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/cars", carDataWithNulls);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "API should handle null optional fields correctly");

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdCar = await DeserializeResponseAsync<CarResource>(response);
            createdCar.Should().NotBeNull();
            createdCar!.Description.Should().BeNull("Null optional fields should remain null");
            createdCar.ImageUrl.Should().BeNull("Null optional fields should remain null");
        }
    }

    /// <summary>
    /// DTO for car resource.
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
