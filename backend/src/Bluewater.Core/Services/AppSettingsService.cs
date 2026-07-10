using Bluewater.Core.Dto.AppSettings;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly BluewaterContext _db;

    public AppSettingsService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<MaterialPlannerSettingsDto> GetMaterialPlannerSettingsAsync()
    {
        var settings = await _db.AppSettings.AsNoTracking().FirstAsync();
        return new MaterialPlannerSettingsDto(settings.MaterialPlannerStartHour, settings.MaterialPlannerEndHour);
    }
}
