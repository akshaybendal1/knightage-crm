using System.Data;
using Dapper;

namespace Knightage.Crm.Infrastructure.Data;

/// <summary>
/// SQL Server's DATETIME2 has no timezone concept, so Dapper reads every value back
/// with DateTime.Kind = Unspecified even though every column here is always written as
/// UTC (DateTime.UtcNow). Left unspecified, System.Text.Json serializes it without a
/// "Z" suffix, and browsers parse that as local time -- silently shifting every
/// timestamp by the client's UTC offset. This marks every DateTime read through Dapper
/// as UTC so serialization is correct.
/// </summary>
public class UtcDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value) => DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);

    public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = value;
}
