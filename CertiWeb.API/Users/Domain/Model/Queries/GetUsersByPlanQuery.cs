namespace CertiWeb.API.Users.Domain.Model.Queries;

/// <summary>
/// Query for retrieving users by their subscription plan.
/// </summary>
/// <param name="Plan">The plan to filter by.</param>
public record GetUsersByPlanQuery(string Plan);
