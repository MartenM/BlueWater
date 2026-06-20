using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> UseBluewater(this WebApplication app)
    {
        // Migrations
        Migrate(app);

        // Development seeding:
        if (app.Environment.IsDevelopment())
        {
            using var scope =  app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<BluewaterContextSeeder>();
            
            await seeder.SeedAsync();
        }
        
        return app;
    }

    private static void Migrate(WebApplication app)
    {
        var mainScope = app.Services.CreateScope();
        var context = mainScope.ServiceProvider.GetRequiredService<BluewaterContext>();
        
        context.Database.Migrate();
    }
}