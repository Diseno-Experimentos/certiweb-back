using CertiWeb.API.Users.Domain.Model.Aggregates;
using CertiWeb.API.Users.Domain.Model.Commands;
using CertiWeb.IntegrationTests.Shared.Infrastructure;

namespace CertiWeb.IntegrationTests.Users.Domain.Model.Aggregates;

[TestFixture]
public class UserIntegrationTests : DatabaseTestBase
{
    private CreateUserCommand _validCommand = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        
        _validCommand = new CreateUserCommand(
            name: "Juan Perez",
            email: "juan.perez@email.com",
            password: "hashedPassword123",
            plan: "Premium"
        );
    }

    [Test]
    public async Task CreateUser_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        var user = new User(_validCommand);

        // Act
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Assert
        var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.email == "juan.perez@email.com");
        savedUser.Should().NotBeNull();
        savedUser!.name.Should().Be("Juan Perez");
        savedUser.email.Should().Be("juan.perez@email.com");
        savedUser.password.Should().Be("hashedPassword123");
        savedUser.plan.Should().Be("Premium");
        savedUser.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateMultipleUsers_WithDifferentData_ShouldPersistAllToDatabase()
    {
        // Arrange
        var users = new List<User>
        {
            new(new CreateUserCommand("Maria Garcia", "maria@email.com", "hash1", "Basic")),
            new(new CreateUserCommand("Carlos Rodriguez", "carlos@email.com", "hash2", "Premium")),
            new(new CreateUserCommand("Ana Lopez", "ana@email.com", "hash3", "Enterprise"))
        };

        // Act
        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        // Assert
        var savedUsers = await Context.Users.ToListAsync();
        savedUsers.Should().HaveCount(3);
        savedUsers.Select(u => u.email).Should().Contain(new[] 
        { 
            "maria@email.com", 
            "carlos@email.com", 
            "ana@email.com" 
        });
    }

    [Test]
    public async Task UpdateUser_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var user = new User(_validCommand);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        user.name = "Juan Carlos Updated";
        user.email = "juan.updated@email.com";
        user.plan = "Enterprise";
        await Context.SaveChangesAsync();

        // Assert
        await SaveChangesAndClearContext();
        var updatedUser = await Context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.name.Should().Be("Juan Carlos Updated");
        updatedUser.email.Should().Be("juan.updated@email.com");
        updatedUser.plan.Should().Be("Enterprise");
    }

    [Test]
    public async Task DeleteUser_ShouldRemoveFromDatabase()
    {
        // Arrange
        var user = new User(_validCommand);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var userId = user.Id;

        // Act
        Context.Users.Remove(user);
        await Context.SaveChangesAsync();

        // Assert
        var deletedUser = await Context.Users.FindAsync(userId);
        deletedUser.Should().BeNull();
    }

    [Test]
    public async Task FindUserById_WithExistingId_ShouldReturnCorrectUser()
    {
        // Arrange
        var user = new User(_validCommand);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var userId = user.Id;

        // Act
        await SaveChangesAndClearContext();
        var foundUser = await Context.Users.FindAsync(userId);

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.name.Should().Be("Juan Perez");
        foundUser.email.Should().Be("juan.perez@email.com");
        foundUser.Id.Should().Be(userId);
    }

    [Test]
    public async Task FindUserById_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = 99999;

        // Act
        var foundUser = await Context.Users.FindAsync(nonExistingId);

        // Assert
        foundUser.Should().BeNull();
    }

    [Test]
    public async Task QueryUsersByEmail_ShouldReturnMatchingUser()
    {
        // Arrange
        var users = new List<User>
        {
            new(new CreateUserCommand("User One", "user1@test.com", "hash1", "Basic")),
            new(new CreateUserCommand("User Two", "user2@test.com", "hash2", "Premium")),
            new(new CreateUserCommand("User Three", "user3@example.com", "hash3", "Basic"))
        };
        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        // Act
        var testUsers = await Context.Users
            .Where(u => u.email.Contains("test.com"))
            .ToListAsync();

        // Assert
        testUsers.Should().HaveCount(2);
        testUsers.Select(u => u.email).Should().Contain(new[] { "user1@test.com", "user2@test.com" });
    }

    [Test]
    public async Task QueryUsersByPlan_ShouldReturnUsersWithSpecificPlan()
    {
        // Arrange
        var users = new List<User>
        {
            new(new CreateUserCommand("Premium User 1", "premium1@email.com", "hash1", "Premium")),
            new(new CreateUserCommand("Basic User", "basic@email.com", "hash2", "Basic")),
            new(new CreateUserCommand("Premium User 2", "premium2@email.com", "hash3", "Premium"))
        };
        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        // Act
        var premiumUsers = await Context.Users
            .Where(u => u.plan == "Premium")
            .ToListAsync();

        // Assert
        premiumUsers.Should().HaveCount(2);
        premiumUsers.Should().OnlyContain(u => u.plan == "Premium");
        premiumUsers.Select(u => u.name).Should().Contain(new[] { "Premium User 1", "Premium User 2" });
    }

    [Test]
    public async Task UserEntityTracking_ShouldDetectChanges()
    {
        // Arrange
        var user = new User(_validCommand);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        user.name = "Updated Name";
        user.plan = "Updated Plan";

        // Assert
        var entry = Context.Entry(user);
        entry.State.Should().Be(EntityState.Modified);
        entry.Property(u => u.name).IsModified.Should().BeTrue();
        entry.Property(u => u.plan).IsModified.Should().BeTrue();
        entry.Property(u => u.email).IsModified.Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentUserCreation_ShouldHandleMultipleOperations()
    {
        // Arrange
        var user1 = new User(new CreateUserCommand("User 1", "user1@concurrent.com", "hash1", "Basic"));
        var user2 = new User(new CreateUserCommand("User 2", "user2@concurrent.com", "hash2", "Premium"));

        // Act
        Context.Users.Add(user1);
        Context.Users.Add(user2);
        await Context.SaveChangesAsync();

        // Assert
        var allUsers = await Context.Users.ToListAsync();
        allUsers.Should().Contain(u => u.email == "user1@concurrent.com");
        allUsers.Should().Contain(u => u.email == "user2@concurrent.com");
    }

    [Test]
    public async Task CreateUser_WithNullAndEmptyValues_ShouldPersistAsExpected()
    {
        // Arrange
        var userWithEmptyValues = new User(new CreateUserCommand("", "", "", ""));

        // Act
        Context.Users.Add(userWithEmptyValues);
        await Context.SaveChangesAsync();

        // Assert
        var savedUser = await Context.Users.FindAsync(userWithEmptyValues.Id);
        savedUser.Should().NotBeNull();
        savedUser!.name.Should().Be("");
        savedUser.email.Should().Be("");
        savedUser.password.Should().Be("");
        savedUser.plan.Should().Be("");
    }

    [Test]
    public async Task QueryUsersByMultipleCriteria_ShouldReturnCorrectResults()
    {
        // Arrange
        var users = new List<User>
        {
            new(new CreateUserCommand("Premium Juan", "juan@premium.com", "hash1", "Premium")),
            new(new CreateUserCommand("Basic Juan", "juan@basic.com", "hash2", "Basic")),
            new(new CreateUserCommand("Premium Maria", "maria@premium.com", "hash3", "Premium"))
        };
        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        // Act
        var premiumJuans = await Context.Users
            .Where(u => u.name.Contains("Juan") && u.plan == "Premium")
            .ToListAsync();

        // Assert
        premiumJuans.Should().HaveCount(1);
        premiumJuans.First().name.Should().Be("Premium Juan");
        premiumJuans.First().email.Should().Be("juan@premium.com");
    }
}
