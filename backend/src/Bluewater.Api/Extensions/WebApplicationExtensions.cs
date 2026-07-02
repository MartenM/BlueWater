using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bluewater.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> UseBluewater(this WebApplication app)
    {
        // Migrations
        Migrate(app);

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<BluewaterContextSeeder>();
        var seedingOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;

        // Development seeding, or forced via config (e.g. for demo environments):
        if (app.Environment.IsDevelopment() || seedingOptions.ForceDevelopmentSeed)
            await seeder.SeedDevelopmentAsync();
        else
            await seeder.SeedProductionAsync();

        return app;
    }

    private static void Migrate(WebApplication app)
    {
        var mainScope = app.Services.CreateScope();
        var context = mainScope.ServiceProvider.GetRequiredService<BluewaterContext>();
        
        context.Database.Migrate();
    }
}