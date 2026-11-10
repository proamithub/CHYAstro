using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Data;

/// <summary>
/// Dapper Type Handlers for modern .NET DateOnly and TimeOnly types.
/// Automatically bridges SQL Server DATE (DateTime) and TIME (TimeSpan) to C# DateOnly and TimeOnly.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool _registered = false;
    private static readonly object _lock = new();

    public static void Register()
    {
        if (_registered) return;
        lock (_lock)
        {
            if (_registered) return;

            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new NullableTimeOnlyTypeHandler());

            _registered = true;
        }
    }
}

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        parameter.DbType = DbType.Date;
    }

    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            string s when DateOnly.TryParse(s, out var d) => d,
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
    }
}

public class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        parameter.DbType = DbType.Date;
    }

    public override DateOnly? Parse(object value)
    {
        if (value == null || value is DBNull) return null;
        return value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            string s when DateOnly.TryParse(s, out var d) => d,
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
    }
}

public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.Value = value.ToTimeSpan();
        parameter.DbType = DbType.Time;
    }

    public override TimeOnly Parse(object value)
    {
        return value switch
        {
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            DateTime dt => TimeOnly.FromDateTime(dt),
            string s when TimeOnly.TryParse(s, out var t) => t,
            _ => TimeOnly.FromTimeSpan((TimeSpan)value)
        };
    }
}

public class NullableTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly?>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
    {
        parameter.Value = value.HasValue ? value.Value.ToTimeSpan() : DBNull.Value;
        parameter.DbType = DbType.Time;
    }

    public override TimeOnly? Parse(object value)
    {
        if (value == null || value is DBNull) return null;
        return value switch
        {
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            DateTime dt => TimeOnly.FromDateTime(dt),
            string s when TimeOnly.TryParse(s, out var t) => t,
            _ => TimeOnly.FromTimeSpan((TimeSpan)value)
        };
    }
}
