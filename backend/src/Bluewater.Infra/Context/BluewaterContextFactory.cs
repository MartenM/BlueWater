using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bluewater.Infra.Context;

/// <summary>
/// Used to configure the context in the Infra project.
/// </summary>
public class BluewaterContextFactory : IDesignTimeDbContextFactory<BluewaterContext>
{
    public BluewaterContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BluewaterContext>();
        optionsBuilder.UseNpgsql();
        
        return new BluewaterContext(optionsBuilder.Options);
    }
}