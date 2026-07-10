using Bluewater.Core.Dto.AppSettings;

namespace Bluewater.Core.Services.Abstractions;

public interface IAppSettingsService
{
    Task<MaterialPlannerSettingsDto> GetMaterialPlannerSettingsAsync();
}
