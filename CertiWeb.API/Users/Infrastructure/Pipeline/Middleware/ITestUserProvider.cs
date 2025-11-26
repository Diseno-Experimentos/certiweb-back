using CertiWeb.API.Users.Domain.Model.Aggregates;

namespace CertiWeb.API.Users.Infrastructure.Pipeline.Middleware;

/// <summary>
/// Optional test service that, when registered, provides a user instance for authorization during system tests.
/// The production pipeline will ignore this unless tests register an implementation.
/// </summary>
public interface ITestUserProvider
{
    Task<User?> GetUserByIdAsync(int id);
}
