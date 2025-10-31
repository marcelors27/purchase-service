using System.Data;
using Dapper;

namespace PurchaseService.Api.Infrastructure;

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) =>
        value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            DateTimeOffset dateTimeOffset => DateOnly.FromDateTime(dateTimeOffset.UtcDateTime),
            string dateString when DateOnly.TryParse(dateString, out var result) => result,
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateOnly.")
        };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}
