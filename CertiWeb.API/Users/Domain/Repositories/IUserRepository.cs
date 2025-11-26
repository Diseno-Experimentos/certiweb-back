using CertiWeb.API.Shared.Domain.Repositories;
using CertiWeb.API.Users.Domain.Model.Aggregates;

namespace CertiWeb.API.Users.Domain.Repositories;

/// <summary>
/// Repository interface for user data access operations.
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> FindUserByEmailAsync(string email);

    /// <summary>
    /// Finds users by their subscription plan.
    /// </summary>
    /// <param name="plan">The plan to filter by.</param>
    /// <returns>A collection of users with the specified plan.</returns>
    Task<IEnumerable<User>> FindUsersByPlanAsync(string plan);
}