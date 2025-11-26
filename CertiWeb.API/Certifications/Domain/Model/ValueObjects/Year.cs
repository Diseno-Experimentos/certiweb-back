namespace CertiWeb.API.Certifications.Domain.Model.ValueObjects;

/// <summary>
/// Represents a vehicle year as a value object.
/// </summary>
public record Year
{
    public int Value { get; }

    public Year(int value)
    {
        var currentYear = DateTime.Now.Year;
        // Accept years from 1900 up to current year + 1 to align with API and system tests
        var minYear = 1900;
        if (value < minYear || value > currentYear + 1)
            throw new ArgumentException($"Year must be between {minYear} and {currentYear + 1}", nameof(value));

        Value = value;
    }

    public static implicit operator int(Year year) => year.Value;
    public static implicit operator Year(int value) => new(value);
}