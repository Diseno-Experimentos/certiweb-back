using Bogus;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Users.Domain.Model.Commands;

namespace CertiWeb.SystemTests.TestData;

/// <summary>
/// Test data builders using Bogus library for generating realistic test data.
/// </summary>
public static class TestDataBuilder
{
    private static readonly Faker _faker = new("es"); // Spanish locale for realistic names

    /// <summary>
    /// Creates a valid CreateUserCommand with realistic data.
    /// </summary>
    public static CreateUserCommand CreateValidUserCommand()
    {
        return new CreateUserCommand(
            name: _faker.Person.FullName,
            email: _faker.Internet.Email(),
            password: _faker.Internet.Password(12),
            plan: _faker.PickRandom("Basic", "Premium", "Enterprise")
        );
    }

    /// <summary>
    /// Creates a CreateUserCommand with specific values.
    /// </summary>
    public static CreateUserCommand CreateUserCommand(
        string? name = null,
        string? email = null,
        string? password = null,
        string? plan = null)
    {
        return new CreateUserCommand(
            name: name ?? _faker.Person.FullName,
            email: email ?? _faker.Internet.Email(),
            password: password ?? _faker.Internet.Password(12),
            plan: plan ?? "Premium"
        );
    }

    /// <summary>
    /// Creates a valid CreateCarCommand with realistic data.
    /// </summary>
    public static CreateCarCommand CreateValidCarCommand(int? brandId = null)
    {
        var carModel = _faker.Vehicle.Model();
        var carBrand = _faker.Vehicle.Manufacturer();
        
        return new CreateCarCommand(
            Title: $"{carBrand} {carModel} {_faker.Date.Recent(365).Year}",
            Owner: _faker.Person.FullName,
            OwnerEmail: _faker.Internet.Email(),
            Year: _faker.Date.Between(new DateTime(2015, 1, 1), DateTime.Now).Year,
            BrandId: brandId ?? 1,
            Model: carModel,
            Description: _faker.Lorem.Sentence(10),
            PdfCertification: GenerateValidBase64(),
            ImageUrl: _faker.Internet.Url(),
            Price: _faker.Random.Decimal(10000, 80000),
            LicensePlate: GenerateValidLicensePlate(),
            OriginalReservationId: _faker.Random.Int(1, 10000)
        );
    }

    /// <summary>
    /// Creates a CreateCarCommand with specific values.
    /// </summary>
    public static CreateCarCommand CreateCarCommand(
        string? title = null,
        string? owner = null,
        string? ownerEmail = null,
        int? year = null,
        int? brandId = null,
        string? model = null,
        string? description = null,
        string? pdfCertification = null,
        string? imageUrl = null,
        decimal? price = null,
        string? licensePlate = null,
        int? originalReservationId = null)
    {
        return new CreateCarCommand(
            Title: title ?? $"{_faker.Vehicle.Manufacturer()} {_faker.Vehicle.Model()}",
            Owner: owner ?? _faker.Person.FullName,
            OwnerEmail: ownerEmail ?? _faker.Internet.Email(),
            Year: year ?? _faker.Date.Recent(365).Year,
            BrandId: brandId ?? 1,
            Model: model ?? _faker.Vehicle.Model(),
            Description: description ?? _faker.Lorem.Sentence(),
            PdfCertification: pdfCertification ?? GenerateValidBase64(),
            ImageUrl: imageUrl ?? _faker.Internet.Url(),
            Price: price ?? _faker.Random.Decimal(15000, 60000),
            LicensePlate: licensePlate ?? GenerateValidLicensePlate(),
            OriginalReservationId: originalReservationId ?? _faker.Random.Int(1, 10000)
        );
    }

    /// <summary>
    /// Generates a valid license plate format.
    /// </summary>
    public static string GenerateValidLicensePlate()
    {
        // Formato peruano: ABC1234
        return $"{_faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")}{_faker.Random.Int(1000, 9999)}";
    }

    /// <summary>
    /// Generates a valid Base64 string for PDF certification.
    /// </summary>
    public static string GenerateValidBase64()
    {
        var bytes = _faker.Random.Bytes(100);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Creates a list of multiple CreateUserCommands.
    /// </summary>
    public static List<CreateUserCommand> CreateMultipleUserCommands(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateValidUserCommand())
            .ToList();
    }

    /// <summary>
    /// Creates a list of multiple CreateCarCommands.
    /// </summary>
    public static List<CreateCarCommand> CreateMultipleCarCommands(int count, int? brandId = null)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateValidCarCommand(brandId))
            .ToList();
    }

    /// <summary>
    /// Creates invalid data for negative testing.
    /// </summary>
    public static class Invalid
    {
        public static CreateUserCommand UserWithEmptyName()
        {
            return CreateUserCommand(name: "");
        }

        public static CreateUserCommand UserWithInvalidEmail()
        {
            return CreateUserCommand(email: "invalid-email");
        }

        public static CreateCarCommand CarWithInvalidYear()
        {
            return CreateCarCommand(year: 1800);
        }

        public static CreateCarCommand CarWithNegativePrice()
        {
            return CreateCarCommand(price: -1000);
        }

        public static CreateCarCommand CarWithInvalidLicensePlate()
        {
            return CreateCarCommand(licensePlate: "AB");
        }

        public static CreateCarCommand CarWithInvalidPdfCertification()
        {
            return CreateCarCommand(pdfCertification: "short");
        }
    }
}
