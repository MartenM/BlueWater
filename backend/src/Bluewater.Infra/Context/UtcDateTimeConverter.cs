using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bluewater.Infra.Context;

/// <summary>
/// Npgsql rejects DateTime with Kind=Unspecified/Local for "timestamp with time zone" columns.
/// Coerces every DateTime to UTC on write and stamps Kind=Utc on read, so callers never have to
/// remember to do this themselves.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(ToUtc, FromUtc)
    {
    }

    private static readonly System.Linq.Expressions.Expression<Func<DateTime, DateTime>> ToUtc = v =>
        v.Kind == DateTimeKind.Utc ? v :
        v.Kind == DateTimeKind.Local ? v.ToUniversalTime() :
        DateTime.SpecifyKind(v, DateTimeKind.Utc);

    private static readonly System.Linq.Expressions.Expression<Func<DateTime, DateTime>> FromUtc = v =>
        DateTime.SpecifyKind(v, DateTimeKind.Utc);
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter() : base(ToUtc, FromUtc)
    {
    }

    private static readonly System.Linq.Expressions.Expression<Func<DateTime?, DateTime?>> ToUtc = v =>
        v == null ? null :
        v.Value.Kind == DateTimeKind.Utc ? v :
        v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() :
        DateTime.SpecifyKind(v.Value, DateTimeKind.Utc);

    private static readonly System.Linq.Expressions.Expression<Func<DateTime?, DateTime?>> FromUtc = v =>
        v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc);
}
