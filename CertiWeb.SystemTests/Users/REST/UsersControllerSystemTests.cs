using CertiWeb.SystemTests.Infrastructure;
using CertiWeb.SystemTests.TestData;

namespace CertiWeb.SystemTests.Users.REST;

[TestFixture]
public class UsersControllerSystemTests : SystemTestBase
{
    [Test]
    public async Task GetAllUsers_ShouldReturnSuccessStatusCode()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetAllUsers_WithEmptyDatabase_ShouldReturnEmptyArray()
    {
        // Arrange - Database is clean from base setup

        // Act
        var response = await Client.GetAsync("/api/v1/users");
        var users = await DeserializeResponseAsync<UserResource[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllUsers_WithExistingUsers_ShouldReturnAllUsers()
    {
        // Arrange - Create test users directly in database
        var testUsers = new List<CertiWeb.API.Users.Domain.Model.Aggregates.User>();
        var userCommands = TestDataBuilder.CreateMultipleUserCommands(3);

        foreach (var command in userCommands)
        {
            testUsers.Add(new CertiWeb.API.Users.Domain.Model.Aggregates.User(command));
        }

        using (var context = GetFreshDbContext())
        {
            context.Users.AddRange(testUsers);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/v1/users");
        var users = await DeserializeResponseAsync<UserResource[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Length.Should().Be(3);
        users.Select(u => u.Email).Should().Contain(userCommands.Select(c => c.email));
    }

    [Test]
    public async Task GetUserById_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var userCommand = TestDataBuilder.CreateValidUserCommand();
        var testUser = new CertiWeb.API.Users.Domain.Model.Aggregates.User(userCommand);

        using (var context = GetFreshDbContext())
        {
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync($"/api/v1/users/{testUser.Id}");
        var user = await DeserializeResponseAsync<UserResource>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        user.Should().NotBeNull();
        user!.Id.Should().Be(testUser.Id);
        user.Name.Should().Be(userCommand.name);
        user.Email.Should().Be(userCommand.email);
    }

    [Test]
    public async Task GetUserById_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = 99999;

        // Act
        var response = await Client.GetAsync($"/api/v1/users/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUserByEmail_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var userCommand = TestDataBuilder.CreateUserCommand(email: "test.user@example.com");
        var testUser = new CertiWeb.API.Users.Domain.Model.Aggregates.User(userCommand);

        using (var context = GetFreshDbContext())
        {
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync($"/api/v1/users/email/{testUser.email}");
        var user = await DeserializeResponseAsync<UserResource>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        user.Should().NotBeNull();
        user!.Email.Should().Be("test.user@example.com");
        user.Name.Should().Be(userCommand.name);
    }

    [Test]
    public async Task GetUserByEmail_WithNonExistingEmail_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingEmail = "nonexisting@example.com";

        // Act
        var response = await Client.GetAsync($"/api/v1/users/email/{nonExistingEmail}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUsersByPlan_WithExistingPlan_ShouldReturnUsersWithThatPlan()
    {
        // Arrange
        var premiumUsers = new List<CertiWeb.API.Users.Domain.Model.Aggregates.User>
        {
            new(TestDataBuilder.CreateUserCommand(plan: "Premium")),
            new(TestDataBuilder.CreateUserCommand(plan: "Premium"))
        };
        var basicUser = new CertiWeb.API.Users.Domain.Model.Aggregates.User(
            TestDataBuilder.CreateUserCommand(plan: "Basic"));

        using (var context = GetFreshDbContext())
        {
            context.Users.AddRange(premiumUsers);
            context.Users.Add(basicUser);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/v1/users/plan/Premium");
        var users = await DeserializeResponseAsync<UserResource[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Length.Should().Be(2);
        users.Should().OnlyContain(u => u.Plan == "Premium");
    }

    [Test]
    public async Task GetUsersByPlan_WithNonExistingPlan_ShouldReturnEmptyArray()
    {
        // Arrange
        var nonExistingPlan = "NonExistingPlan";

        // Act
        var response = await Client.GetAsync($"/api/v1/users/plan/{nonExistingPlan}");
        var users = await DeserializeResponseAsync<UserResource[]>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Should().BeEmpty();
    }

    [Test]
    public async Task UserEndpoints_ConcurrentRequests_ShouldHandleMultipleClients()
    {
        // Arrange
        var userCommand = TestDataBuilder.CreateValidUserCommand();
        var testUser = new CertiWeb.API.Users.Domain.Model.Aggregates.User(userCommand);

        using (var context = GetFreshDbContext())
        {
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        var tasks = new List<Task<HttpResponseMessage>>();
        const int numberOfConcurrentRequests = 5;

        // Act
        for (int i = 0; i < numberOfConcurrentRequests; i++)
        {
            tasks.Add(Client.GetAsync("/api/v1/users"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(numberOfConcurrentRequests);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    [Test]
    public async Task UserApiFlow_CompleteReadOperations_ShouldWorkEndToEnd()
    {
        // Arrange
        var userCommands = new[]
        {
            TestDataBuilder.CreateUserCommand(name: "John Doe", email: "john@example.com", plan: "Premium"),
            TestDataBuilder.CreateUserCommand(name: "Jane Smith", email: "jane@example.com", plan: "Basic"),
            TestDataBuilder.CreateUserCommand(name: "Bob Wilson", email: "bob@example.com", plan: "Premium")
        };

        var testUsers = userCommands.Select(cmd => new CertiWeb.API.Users.Domain.Model.Aggregates.User(cmd)).ToList();

        using (var context = GetFreshDbContext())
        {
            context.Users.AddRange(testUsers);
            await context.SaveChangesAsync();
        }

        // Act & Assert - Get All Users
        var getAllResponse = await Client.GetAsync("/api/v1/users");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var allUsers = await DeserializeResponseAsync<UserResource[]>(getAllResponse);
        allUsers!.Length.Should().Be(3);

        // Act & Assert - Get User by ID
        var firstUser = testUsers.First();
        var getByIdResponse = await Client.GetAsync($"/api/v1/users/{firstUser.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userById = await DeserializeResponseAsync<UserResource>(getByIdResponse);
        userById!.Name.Should().Be("John Doe");

        // Act & Assert - Get User by Email
        var getByEmailResponse = await Client.GetAsync($"/api/v1/users/email/jane@example.com");
        getByEmailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userByEmail = await DeserializeResponseAsync<UserResource>(getByEmailResponse);
        userByEmail!.Name.Should().Be("Jane Smith");

        // Act & Assert - Get Users by Plan
        var getByPlanResponse = await Client.GetAsync("/api/v1/users/plan/Premium");
        getByPlanResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var premiumUsers = await DeserializeResponseAsync<UserResource[]>(getByPlanResponse);
        premiumUsers!.Length.Should().Be(2);
        premiumUsers.Select(u => u.Name).Should().Contain(new[] { "John Doe", "Bob Wilson" });
    }

    [Test]
    public async Task UserEndpoints_ShouldHaveCorrectResponseHeaders()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Should().ContainKey("Date");
        response.Headers.CacheControl?.NoCache.Should().BeFalse(); // API should be cacheable
    }

    /// <summary>
    /// DTO for user response deserialization.
    /// </summary>
    private record UserResource(
        int Id,
        string Name,
        string Email,
        string Plan
    );
}
