using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class SeasonServiceTests : SqliteServiceTestBase
{
    private readonly ISeasonService _sut;

    public SeasonServiceTests()
    {
        _sut = GetService<ISeasonService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoSeasonsExist()
    {
        var result = await _sut.ListAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ReturnsSeasons_MostRecentFirst_FlaggingCurrent()
    {
        var current = await CreateCurrentSeasonAsync(
            startDate: new DateOnly(2025, 6, 1),
            endDate: new DateOnly(2026, 5, 31));
        var older = new Bluewater.Domain.Models.BlueSeason
        {
            Id = Guid.NewGuid(),
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2025, 5, 31)
        };
        Db.Seasons.Add(older);
        await Db.SaveChangesAsync();

        var result = await _sut.ListAsync();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(current.Id);
        result[0].IsCurrent.ShouldBeTrue();
        result[0].Name.ShouldBe("2025/2026");
        result[1].Id.ShouldBe(older.Id);
        result[1].IsCurrent.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_ReturnsSeason_FlaggingCurrent()
    {
        var current = await CreateCurrentSeasonAsync(
            startDate: new DateOnly(2025, 6, 1),
            endDate: new DateOnly(2026, 5, 31));

        var result = await _sut.GetAsync(current.Id);

        result.Id.ShouldBe(current.Id);
        result.IsCurrent.ShouldBeTrue();
        result.Name.ShouldBe("2025/2026");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenSeasonDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }
}
