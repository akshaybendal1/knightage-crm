using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class MeetingRepository : IMeetingRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public MeetingRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Meeting>> GetByLeadIdAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        var meetings = await connection.QueryAsync<Meeting>("usp_Meetings_GetByLeadId",
            new { LeadId = leadId, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return meetings.ToList();
    }

    public async Task<Meeting?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Meeting>("usp_Meetings_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<Meeting> CreateAsync(Meeting meeting)
    {
        meeting.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Meetings_Create", new
        {
            meeting.Id, meeting.OrganizationId, meeting.LeadId, meeting.Title, meeting.Description,
            meeting.StartAtUtc, meeting.EndAtUtc, meeting.Location, meeting.Status, meeting.OwnerUserId,
            meeting.GoogleEventId, meeting.GoogleSyncStatus, meeting.CreatedByUserId, meeting.CreatedAtUtc
        }, commandType: CommandType.StoredProcedure);
        return meeting;
    }

    public async Task UpdateAsync(Meeting meeting)
    {
        meeting.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Meetings_Update", new
        {
            meeting.Id, meeting.OrganizationId, meeting.Title, meeting.Description, meeting.StartAtUtc,
            meeting.EndAtUtc, meeting.Location, meeting.UpdatedByUserId, meeting.UpdatedAtUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? cancelledAtUtc, string? updatedByUserId, DateTime updatedAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Meetings_UpdateStatus", new
        {
            Id = id,
            Status = status,
            CancelledAtUtc = cancelledAtUtc,
            UpdatedByUserId = updatedByUserId,
            UpdatedAtUtc = updatedAtUtc,
            OrganizationId = _tenantContext.RequireOrganizationId()
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateGoogleSyncAsync(Guid id, string? googleEventId, string googleSyncStatus, DateTime? errorAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Meetings_UpdateGoogleSync", new
        {
            Id = id,
            GoogleEventId = googleEventId,
            GoogleSyncStatus = googleSyncStatus,
            ErrorAtUtc = errorAtUtc,
            OrganizationId = _tenantContext.RequireOrganizationId()
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<Meeting>> GetStartingSoonUnremindedAsync(DateTime nowUtc, DateTime untilUtc)
    {
        using var connection = _context.CreateConnection();
        var meetings = await connection.QueryAsync<Meeting>("usp_Meetings_GetStartingSoonUnreminded",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), NowUtc = nowUtc, UntilUtc = untilUtc }, commandType: CommandType.StoredProcedure);
        return meetings.ToList();
    }

    public async Task MarkRemindedAsync(IEnumerable<Guid> meetingIds, DateTime sentAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Meetings_MarkReminded", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            MeetingIds = string.Join(",", meetingIds),
            SentAtUtc = sentAtUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<Meeting>> GetUpcomingForOwnerAsync(string ownerUserId, DateTime nowUtc, DateTime untilUtc)
    {
        using var connection = _context.CreateConnection();
        var meetings = await connection.QueryAsync<Meeting>("usp_Meetings_GetUpcomingForOwner",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), OwnerUserId = ownerUserId, NowUtc = nowUtc, UntilUtc = untilUtc },
            commandType: CommandType.StoredProcedure);
        return meetings.ToList();
    }
}
