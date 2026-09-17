using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

// The whitelisted set of group-by fields a custom report can use. Deliberately not an
// open-ended field picker -- every other query in this codebase is a fixed, hand-written
// string (no precedent for dynamic/user-supplied field names reaching SQL), so
// CustomReportRepository maps these keys through a hardcoded switch rather than ever
// building SQL from client-supplied identifiers.
public static class CustomReportDimensions
{
    public const string Lead = "lead";
    public const string Stage = "stage";
    public const string Owner = "owner";
    public const string Product = "product";
    public const string Category = "category";
    public const string Source = "source";
    public const string CreatedDate = "createdDate";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Lead, Stage, Owner, Product, Category, Source, CreatedDate };
}

// Resolved, validated query handed to the repository -- CustomReportsController builds
// this from the wire-shaped CustomReportDefinition (whitelist-checked dimensions, range
// resolved to Since/Until via ReportsController.ResolveRange, same as every other report).
public record CustomReportQuery(
    IReadOnlyList<string> Dimensions,
    DateTime? SinceUtc,
    DateTime? UntilUtc,
    bool BucketByMonth,
    IReadOnlyList<Guid>? StageIds,
    IReadOnlyList<string>? OwnerUserIds,
    IReadOnlyList<Guid>? ProductIds,
    IReadOnlyList<string>? Sources);

public interface ICustomReportRepository
{
    Task<IReadOnlyList<CustomReportRow>> RunAsync(CustomReportQuery query);
}
