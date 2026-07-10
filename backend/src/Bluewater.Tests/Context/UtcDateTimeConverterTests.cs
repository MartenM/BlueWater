using Bluewater.Infra.Context;
using Shouldly;

namespace Bluewater.Tests.Context;

/// <summary>
/// Exercises the model-wide DateTime-to-UTC converter registered via BluewaterContext.ConfigureConventions,
/// which exists to stop Npgsql's "Kind=Unspecified not supported for timestamptz" error.
/// </summary>
public class UtcDateTimeConverterTests
{
    [Fact]
    public void ConvertToProvider_UnspecifiedKind_IsStampedUtc_WithoutShiftingClockTime()
    {
        var converter = new UtcDateTimeConverter();
        var input = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Unspecified);

        var result = (DateTime)converter.ConvertToProvider(input)!;

        result.Kind.ShouldBe(DateTimeKind.Utc);
        result.ShouldBe(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ConvertToProvider_LocalKind_IsConvertedToUtc()
    {
        var converter = new UtcDateTimeConverter();
        var input = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Local);

        var result = (DateTime)converter.ConvertToProvider(input)!;

        result.Kind.ShouldBe(DateTimeKind.Utc);
        result.ShouldBe(input.ToUniversalTime());
    }

    [Fact]
    public void ConvertToProvider_UtcKind_PassesThroughUnchanged()
    {
        var converter = new UtcDateTimeConverter();
        var input = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        var result = (DateTime)converter.ConvertToProvider(input)!;

        result.ShouldBe(input);
    }

    [Fact]
    public void ConvertFromProvider_StampsUtcKind()
    {
        var converter = new UtcDateTimeConverter();
        var stored = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Unspecified);

        var result = (DateTime)converter.ConvertFromProvider(stored)!;

        result.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Nullable_ConvertToProvider_NullPassesThrough()
    {
        var converter = new NullableUtcDateTimeConverter();

        var result = converter.ConvertToProvider(null);

        result.ShouldBeNull();
    }

    [Fact]
    public void Nullable_ConvertToProvider_UnspecifiedKind_IsStampedUtc()
    {
        var converter = new NullableUtcDateTimeConverter();
        DateTime? input = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Unspecified);

        var result = (DateTime?)converter.ConvertToProvider(input);

        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.ShouldBe(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
    }
}
