namespace CertiWeb.API.Certifications.Domain.Model.ValueObjects;

/// <summary>
/// Represents a vehicle license plate as a value object.
/// </summary>
public record LicensePlate
{
    public string Value { get; }

    public LicensePlate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("License plate cannot be null or whitespace", nameof(value));
        // Only allow alpha-numeric characters
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Za-z0-9-]+$"))
            throw new ArgumentException("License plate contains invalid characters", nameof(value));

        // Different rules depending on composition:
        // - If letters-only or numbers-only: allow 3..15 characters
        // - If mixed letters/digits without hyphen: require 6..10 characters
        // - If contains hyphen: allow 3..15 characters
        var cleaned = value.Trim();
        bool lettersOnly = System.Text.RegularExpressions.Regex.IsMatch(cleaned, "^[A-Za-z]+$");
        bool digitsOnly = System.Text.RegularExpressions.Regex.IsMatch(cleaned, "^[0-9]+$");
        bool containsLetters = System.Text.RegularExpressions.Regex.IsMatch(cleaned, "[A-Za-z]");
        bool containsDigits = System.Text.RegularExpressions.Regex.IsMatch(cleaned, "[0-9]");
        bool containsHyphen = cleaned.Contains('-');

        if (containsHyphen)
        {
            if (cleaned.Length < 3 || cleaned.Length > 15)
                throw new ArgumentException("License plate must be between 3 and 15 characters", nameof(value));
        }
        else if (lettersOnly || digitsOnly)
        {
            if (cleaned.Length < 3 || cleaned.Length > 15)
                throw new ArgumentException("License plate must be between 3 and 15 characters", nameof(value));
        }
        else if (containsLetters && containsDigits)
        {
            // mixed letters/digits without hyphen require 6..10 characters
            if (cleaned.Length < 6 || cleaned.Length > 10)
                throw new ArgumentException("License plate must be between 3 and 15 characters", nameof(value));
        }

        Value = cleaned.ToUpperInvariant();
    }

    public static implicit operator string(LicensePlate licensePlate) => licensePlate.Value;
    public static implicit operator LicensePlate(string value) => new(value);
}