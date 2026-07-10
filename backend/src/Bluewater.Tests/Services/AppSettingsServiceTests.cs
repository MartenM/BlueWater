using Bluewater.Core.Services.Abstractions;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class AppSettingsServiceTests : SqliteServiceTestBase
{
    private readonly IAppSettingsService _sut;

    public AppSettingsServiceTests()
    {
        _sut = GetService<IAppSettingsService>();
    }

    [Fact]
    public async Task GetMaterialPlannerSettingsAsync_ReturnsSeededDefaults()
    {
        await CreateCurrentSeasonAsync();

        var result = await _sut.GetMaterialPlannerSettingsAsync();

        result.StartHour.ShouldBe(6);
        result.EndHour.ShouldBe(23);
    }
}
