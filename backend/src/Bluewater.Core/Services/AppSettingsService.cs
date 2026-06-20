using Bluewater.Domain.Models;
using Bluewater.Infra.Context;

namespace Bluewater.Core.Services;

public class AppSettingsService
{
    private BlueAppSettings _appSettings;

    public AppSettingsService(BluewaterContext context)
    {
        _appSettings = context.AppSettings.First();
    }
}