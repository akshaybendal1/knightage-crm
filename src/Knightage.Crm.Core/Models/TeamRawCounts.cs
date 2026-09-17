namespace Knightage.Crm.Core.Models;

// Per-user-id counts straight out of the DB, before display names are resolved --
// that resolution needs a cross-service call to knightage-identity (user data isn't
// stored locally), which is the service layer's job, not the repository's.
public class TeamRawCounts
{
    public IReadOnlyList<UserCount> LeadsCreated { get; set; } = Array.Empty<UserCount>();
    public IReadOnlyList<UserCount> TasksCompleted { get; set; } = Array.Empty<UserCount>();
    public IReadOnlyList<UserCount> TasksOverdue { get; set; } = Array.Empty<UserCount>();
}
