using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Domain.Model.ValueObjects;

[TestFixture]
public class PriceTests
{
    [Test]
    public void Constructor_WithValidValueAndCurrency_ShouldCreatePriceSuccessfully()
    {
        // Arrange
        var value = 15000.50m;
        var currency = "USD";

        // Act
        var price = new Price(value, currency);

        // Assert
        price.Value.Should().Be(value);
        price.Currency.Should().Be(currency);
    }

    [Test]
    public void Constructor_WithValidValueOnly_ShouldCreatePriceWithDefaultCurrency()
    {
        // Arrange
        var value = 25000.75m;

        // Act
        var price = new Price(value);

        // Assert
        price.Value.Should().Be(value);
        price.Currency.Should().Be("SOL");
    }

    [Test]
    public void Constructor_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var negativeValue = -1000m;
        var currency = "USD";

        // Act & Assert
        var action = () => new Price(negativeValue, currency);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Price must be greater than or equal to zero (Parameter 'value')");
    }

    [Test]
    public void Constructor_WithZeroValue_ShouldCreatePriceSuccessfully()
    {
        // Arrange
        var zeroValue = 0m;
        var currency = "EUR";

        // Act
        var price = new Price(zeroValue, currency);

        // Assert
        price.Value.Should().Be(zeroValue);
        price.Currency.Should().Be(currency);
    }

    [Test]
    public void Constructor_WithEmptyCurrency_ShouldThrowArgumentException()
    {
        // Arrange
        var value = 5000m;
        var emptyCurrency = "";

        // Act & Assert
        var action = () => new Price(value, emptyCurrency);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Currency cannot be empty (Parameter 'currency')");
    }

    [Test]
    public void Constructor_WithNullCurrency_ShouldThrowArgumentException()
    {
        // Arrange
        var value = 5000m;
        string? nullCurrency = null;

        // Act & Assert
        var action = () => new Price(value, nullCurrency!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Currency cannot be empty (Parameter 'currency')");
    }

    [Test]
    public void Constructor_WithWhitespaceCurrency_ShouldThrowArgumentException()
    {
        // Arrange
        var value = 5000m;
        var whitespaceCurrency = "   ";

        // Act & Assert
        var action = () => new Price(value, whitespaceCurrency);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Currency cannot be empty (Parameter 'currency')");
    }

    [Test]
    public void ImplicitConversion_FromPriceToDecimal_ShouldReturnCorrectValue()
    {
        // Arrange
        var price = new Price(12345.67m, "PEN");

        // Act
        decimal value = price;

        // Assert
        value.Should().Be(12345.67m);
    }

    [Test]
    public void ImplicitConversion_FromDecimalToPrice_ShouldCreatePriceWithDefaultCurrency()
    {
        // Arrange
        var value = 9876.54m;

        // Act
        Price price = value;

        // Assert
        price.Value.Should().Be(value);
        price.Currency.Should().Be("SOL");
    }

    [TestCase(0.01, "USD")]
    [TestCase(999999.99, "EUR")]
    [TestCase(50000, "SOL")]
    [TestCase(1, "PEN")]
    public void Constructor_WithValidBoundaryValues_ShouldCreatePriceSuccessfully(decimal value, string currency)
    {
        // Arrange & Act
        var price = new Price(value, currency);

        // Assert
        price.Value.Should().Be(value);
        price.Currency.Should().Be(currency);
    }
}
