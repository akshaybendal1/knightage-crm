namespace Knightage.Crm.Core.Models;

public class TeamPerformanceRow
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int LeadsCreated { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksOverdue { get; set; }
}
