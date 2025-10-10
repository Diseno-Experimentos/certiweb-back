using CertiWeb.API.Users.Domain.Model.Aggregates;
using CertiWeb.API.Users.Domain.Model.Commands;
using FluentAssertions;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Users.Domain.Model.Aggregates;

[TestFixture]
public class UserTests
{
    private CreateUserCommand _validCommand;

    [SetUp]
    public void SetUp()
    {
        _validCommand = new CreateUserCommand(
            name: "Juan Perez",
            email: "juan.perez@email.com",
            password: "hashedPassword123",
            plan: "Premium"
        );
    }

    [Test]
    public void DefaultConstructor_ShouldCreateUserWithEmptyProperties()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().Be(0);
        user.name.Should().Be(string.Empty);
        user.email.Should().Be(string.Empty);
        user.password.Should().Be(string.Empty);
        user.plan.Should().Be(string.Empty);
    }

    [Test]
    public void Constructor_WithValidCommand_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var user = new User(command);

        // Assert
        user.name.Should().Be(command.name);
        user.email.Should().Be(command.email);
        user.password.Should().Be(command.password);
        user.plan.Should().Be(command.plan);
        user.Id.Should().Be(0); // Default value for new entity
    }

    [Test]
    public void Constructor_WithDifferentValidCommand_ShouldCreateUserWithCorrectProperties()
    {
        // Arrange
        var command = new CreateUserCommand(
            name: "Maria Garcia",
            email: "maria.garcia@test.com",
            password: "securePassword456",
            plan: "Basic"
        );

        // Act
        var user = new User(command);

        // Assert
        user.name.Should().Be("Maria Garcia");
        user.email.Should().Be("maria.garcia@test.com");
        user.password.Should().Be("securePassword456");
        user.plan.Should().Be("Basic");
    }

    [Test]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var user = new User();
        var newName = "Carlos Rodriguez";
        var newEmail = "carlos.rodriguez@email.com";
        var newPassword = "newHashedPassword789";
        var newPlan = "Enterprise";

        // Act
        user.name = newName;
        user.email = newEmail;
        user.password = newPassword;
        user.plan = newPlan;

        // Assert
        user.name.Should().Be(newName);
        user.email.Should().Be(newEmail);
        user.password.Should().Be(newPassword);
        user.plan.Should().Be(newPlan);
    }

    [Test]
    public void Constructor_WithEmptyStringValues_ShouldCreateUserWithEmptyProperties()
    {
        // Arrange
        var command = new CreateUserCommand(
            name: "",
            email: "",
            password: "",
            plan: ""
        );

        // Act
        var user = new User(command);

        // Assert
        user.name.Should().Be("");
        user.email.Should().Be("");
        user.password.Should().Be("");
        user.plan.Should().Be("");
    }

    [Test]
    public void Constructor_WithVariousPlans_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var basicPlanCommand = _validCommand with { plan = "Basic" };
        var premiumPlanCommand = _validCommand with { plan = "Premium" };
        var enterprisePlanCommand = _validCommand with { plan = "Enterprise" };

        // Act
        var basicUser = new User(basicPlanCommand);
        var premiumUser = new User(premiumPlanCommand);
        var enterpriseUser = new User(enterprisePlanCommand);

        // Assert
        basicUser.plan.Should().Be("Basic");
        premiumUser.plan.Should().Be("Premium");
        enterpriseUser.plan.Should().Be("Enterprise");
    }

    [Test]
    public void Constructor_WithLongValidValues_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var command = new CreateUserCommand(
            name: "Juan Carlos Rodriguez Martinez de la Fuente",
            email: "juan.carlos.rodriguez.martinez.de.la.fuente@verylongdomain.com",
            password: "VeryLongAndSecureHashedPasswordWith123NumbersAndSpecialChars!@#",
            plan: "Premium Enterprise Extended Plan"
        );

        // Act
        var user = new User(command);

        // Assert
        user.name.Should().Be(command.name);
        user.email.Should().Be(command.email);
        user.password.Should().Be(command.password);
        user.plan.Should().Be(command.plan);
    }

    [Test]
    public void Id_Property_ShouldBeReadOnly()
    {
        // Arrange
        var user = new User(_validCommand);

        // Act & Assert
        // The Id property should be get-only, so this test verifies it exists and is readable
        user.Id.Should().Be(0);
    }

    [TestCase("Free")]
    [TestCase("Basic")]
    [TestCase("Premium")]
    [TestCase("Enterprise")]
    [TestCase("Custom")]
    public void Constructor_WithDifferentValidPlans_ShouldCreateUserSuccessfully(string plan)
    {
        // Arrange
        var command = _validCommand with { plan = plan };

        // Act
        var user = new User(command);

        // Assert
        user.plan.Should().Be(plan);
        user.name.Should().Be(_validCommand.name);
        user.email.Should().Be(_validCommand.email);
    }
}
