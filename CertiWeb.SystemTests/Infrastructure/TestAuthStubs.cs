using CertiWeb.API.Users.Application.Internal.OutboundServices;
using CertiWeb.API.Users.Domain.Services;
using CertiWeb.API.Users.Domain.Model.Aggregates;
using CertiWeb.API.Users.Domain.Model.Queries;

namespace CertiWeb.SystemTests.Infrastructure;

/// <summary>
/// Lightweight token service used in system tests to bypass real token validation.
/// </summary>
public class TestTokenService : ITokenService
{
    public string GenerateToken(User user)
    {
        return "test-token";
    }

    public Task<int?> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult<int?>(null);

        // Always return a fixed user id for tests
        return Task.FromResult<int?>(1);
    }
}

/// <summary>
/// Lightweight user query service used in system tests to provide a predictable user.
/// </summary>
public class TestUserQueryService : IUserQueryService
{
    public Task<User?> Handle(GetUserByEmailAndPassword query)
    {
        // Return a test user for any credentials
        var user = new User();
        return Task.FromResult<User?>(user);
    }

    public Task<User?> Handle(GetUserByEmail query)
    {
        var user = new User();
        return Task.FromResult<User?>(user);
    }

    public Task<User?> Handle(GetUserByIdQuery query)
    {
        var user = new User();
        return Task.FromResult<User?>(user);
    }

    public Task<IEnumerable<User>> Handle(GetAllUsersQuery query)
    {
        var user = new User();
        return Task.FromResult<IEnumerable<User>>(new[] { user });
    }

    public Task<IEnumerable<User>> Handle(GetUsersByPlanQuery query)
    {
        var user = new User();
        // Return a single user for any plan in tests
        return Task.FromResult<IEnumerable<User>>(new[] { user });
    }
}

/// <summary>
/// Test user provider used only by the authorization middleware during system tests.
/// It delegates to the real IUserQueryService so behavior stays consistent with the database.
/// </summary>
public class TestUserProvider : CertiWeb.API.Users.Infrastructure.Pipeline.Middleware.ITestUserProvider
{
    private readonly CertiWeb.API.Users.Domain.Services.IUserQueryService _userQueryService;

    public TestUserProvider(CertiWeb.API.Users.Domain.Services.IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            var q = new GetUserByIdQuery(id);
            var user = await _userQueryService.Handle(q);
            // If user wasn't found in DB (tests often run without seeded users), provide a lightweight test user
            if (user == null)
            {
                var fallbackUser = new User();
                fallbackUser.name = "Test User";
                fallbackUser.email = "test@local";
                fallbackUser.password = "not_used";
                fallbackUser.plan = "free";
                return fallbackUser;
            }

            return user;
        }
        catch
        {
            // In system test scenarios there might be transient issues reading from the DB (SQLite in-memory
            // can throw under heavy parallel load when contexts are not shared). Avoid failing the whole
            // request due to middleware lookup problems by falling back to a lightweight test user.
            var fallbackUser = new User();
            fallbackUser.name = "Test User";
            fallbackUser.email = "test@local";
            fallbackUser.password = "not_used";
            fallbackUser.plan = "free";
            return fallbackUser;
        }
    }
}
