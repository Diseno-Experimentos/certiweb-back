namespace CertiWeb.API.Certifications.Domain.Model.ValueObjects;

/// <summary>
/// Represents a price as a value object.
/// </summary>
public record Price
{
    public decimal Value { get; }
    public string Currency { get; }

    public Price(decimal value, string currency)
    {
        if (value < 0)
            throw new ArgumentException("Price must be greater than or equal to zero", nameof(value));

        if (value > 9999999.99m)
            throw new ArgumentException("Price exceeds maximum allowed value", nameof(value));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        // Round to two decimals to match presentation requirements
        Value = Math.Round(value, 2);
        Currency = currency;
    }

    public Price(decimal value) : this(value, "SOL")
    {
    }

    public static implicit operator decimal(Price price) => price.Value;
    public static implicit operator Price(decimal value) => new(value, "SOL");
}