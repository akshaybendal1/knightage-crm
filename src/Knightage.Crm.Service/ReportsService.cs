using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Service;

public class ReportsService : IReportsService
{
    private readonly IReportsRepository _reportsRepository;
    private readonly IIdentityUserDirectory _identityDirectory;
    private readonly IReportExcelExporter _excelExporter;

    public ReportsService(IReportsRepository reportsRepository, IIdentityUserDirectory identityDirectory, IReportExcelExporter excelExporter)
    {
        _reportsRepository = reportsRepository;
        _identityDirectory = identityDirectory;
        _excelExporter = excelExporter;
    }

    public Task<IReadOnlyList<LeadTrendPoint>> GetLeadTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth) =>
        _reportsRepository.GetLeadTrendAsync(sinceUtc, untilUtc, bucketByMonth);

    public Task<IReadOnlyList<LeadSourceBreakdownItem>> GetLeadSourceBreakdownAsync(DateTime? sinceUtc, DateTime? untilUtc) =>
        _reportsRepository.GetLeadSourceBreakdownAsync(sinceUtc, untilUtc);

    public Task<IReadOnlyList<TaskTrendPoint>> GetTaskCompletionTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth) =>
        _reportsRepository.GetTaskCompletionTrendAsync(sinceUtc, untilUtc, bucketByMonth);

    public async Task<IReadOnlyList<TeamPerformanceRow>> GetTeamPerformanceAsync(DateTime? sinceUtc, DateTime? untilUtc, string? authorizationHeader)
    {
        var raw = await _reportsRepository.GetTeamRawCountsAsync(sinceUtc, untilUtc, DateTime.UtcNow.Date);
        // Case-insensitive comparer: user ids are opaque NVARCHAR(100) strings in this DB
        // (no local Users table, no Guid type), and nothing guarantees every writer of
        // CreatedByUserId/AssignedToUserId used the same casing as knightage-identity's
        // own Id -- a case-sensitive lookup here would silently drop real activity rather
        // than error, which is worse than being lenient about casing.
        var users = (await _identityDirectory.GetOrganizationUsersAsync(authorizationHeader))
            .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

        var leadsByUser = raw.LeadsCreated.ToDictionary(c => c.UserId, c => c.Count, StringComparer.OrdinalIgnoreCase);
        var completedByUser = raw.TasksCompleted.ToDictionary(c => c.UserId, c => c.Count, StringComparer.OrdinalIgnoreCase);
        var overdueByUser = raw.TasksOverdue.ToDictionary(c => c.UserId, c => c.Count, StringComparer.OrdinalIgnoreCase);

        // Every user id that showed up in any of the three counts, so someone who only
        // completed tasks (no leads created in range) still gets a row and vice versa.
        var userIds = leadsByUser.Keys
            .Concat(completedByUser.Keys)
            .Concat(overdueByUser.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var rows = new List<TeamPerformanceRow>();
        foreach (var userId in userIds)
        {
            // A user id with no matching directory entry (removed from the org since,
            // or the identity call failed for just this one) is skipped rather than
            // shown with a blank name -- their activity still counts everywhere else,
            // just not surfaced as an unnamed row here.
            if (!users.TryGetValue(userId, out var user))
            {
                continue;
            }

            rows.Add(new TeamPerformanceRow
            {
                UserId = userId,
                DisplayName = user.DisplayName,
                LeadsCreated = leadsByUser.GetValueOrDefault(userId),
                TasksCompleted = completedByUser.GetValueOrDefault(userId),
                TasksOverdue = overdueByUser.GetValueOrDefault(userId),
            });
        }

        return rows
            .OrderByDescending(r => r.LeadsCreated + r.TasksCompleted)
            .ToList();
    }

    public Task<IReadOnlyList<LeadsByProductItem>> GetLeadsByProductAsync(DateTime? sinceUtc, DateTime? untilUtc) =>
        _reportsRepository.GetLeadsByProductAsync(sinceUtc, untilUtc);

    public Task<IReadOnlyList<LeadsByCategoryItem>> GetLeadsByCategoryAsync(DateTime? sinceUtc, DateTime? untilUtc) =>
        _reportsRepository.GetLeadsByCategoryAsync(sinceUtc, untilUtc);

    public async Task<IReadOnlyList<LeadsByOwnerRow>> GetLeadsByOwnerAsync(DateTime? sinceUtc, DateTime? untilUtc, string? authorizationHeader)
    {
        var raw = await _reportsRepository.GetLeadsByOwnerRawAsync(sinceUtc, untilUtc);
        var users = (await _identityDirectory.GetOrganizationUsersAsync(authorizationHeader))
            .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

        var rows = new List<LeadsByOwnerRow>();
        foreach (var count in raw)
        {
            if (string.IsNullOrEmpty(count.UserId))
            {
                rows.Add(new LeadsByOwnerRow { OwnerUserId = null, OwnerName = "Unassigned", LeadCount = count.Count });
                continue;
            }

            // Unlike GetTeamPerformanceAsync, unmatched user ids are NOT skipped here --
            // this report's rows should sum to the total lead count in range, and silently
            // dropping a bucket would make the totals not add up. Falls back to a shortened
            // id label instead of disappearing.
            var name = users.TryGetValue(count.UserId, out var user)
                ? user.DisplayName
                : $"User {count.UserId[..Math.Min(8, count.UserId.Length)]}";
            rows.Add(new LeadsByOwnerRow { OwnerUserId = count.UserId, OwnerName = name, LeadCount = count.Count });
        }

        return rows.OrderByDescending(r => r.LeadCount).ToList();
    }

    public Task<IReadOnlyList<LeadSourceDetailItem>> GetLeadDetailsBySourceAsync(string source, DateTime? sinceUtc, DateTime? untilUtc) =>
        _reportsRepository.GetLeadDetailsBySourceAsync(source, sinceUtc, untilUtc);

    public async Task<byte[]> ExportReportAsync(string reportKey, DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth, string? authorizationHeader) =>
        reportKey switch
        {
            "lead-trends" => _excelExporter.BuildLeadTrend(await GetLeadTrendAsync(sinceUtc, untilUtc, bucketByMonth)),
            "lead-sources" => _excelExporter.BuildLeadSources(await GetLeadSourceBreakdownAsync(sinceUtc, untilUtc)),
            "team-performance" => _excelExporter.BuildTeamPerformance(await GetTeamPerformanceAsync(sinceUtc, untilUtc, authorizationHeader)),
            "task-trends" => _excelExporter.BuildTaskTrend(await GetTaskCompletionTrendAsync(sinceUtc, untilUtc, bucketByMonth)),
            "leads-by-product" => _excelExporter.BuildLeadsByProduct(await GetLeadsByProductAsync(sinceUtc, untilUtc)),
            "leads-by-category" => _excelExporter.BuildLeadsByCategory(await GetLeadsByCategoryAsync(sinceUtc, untilUtc)),
            "leads-by-owner" => _excelExporter.BuildLeadsByOwner(await GetLeadsByOwnerAsync(sinceUtc, untilUtc, authorizationHeader)),
            _ => throw new ArgumentException($"Unknown report key: {reportKey}", nameof(reportKey)),
        };
}
