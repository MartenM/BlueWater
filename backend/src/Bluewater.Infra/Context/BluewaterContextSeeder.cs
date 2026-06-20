using Bluewater.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bluewater.Infra.Context;

public class BluewaterContextSeeder
{
    public BluewaterContextSeeder(BluewaterContext context, UserManager<BlueUser> userManager, ILogger<BluewaterContextSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    private BluewaterContext _context;
    private UserManager<BlueUser> _userManager;
    private ILogger<BluewaterContextSeeder> _logger;

    public async Task SeedAsync()
    {
        if (_context.Users.Any())
        {
            _logger.LogInformation("No database seeding. Users already exists.");
            return;
        }
        
        _logger.LogInformation("Seeding database...");

        var res = await _userManager.CreateAsync(new BlueUser()
        {
            Email = "admin@example.com",
            UserName = "admin",
            EmailConfirmed = true,
        }, "admin");
        VerifyResult(res);
        
        // Create season
        var currentYear = DateTime.Now.Year;
        
        var seasonResult = _context.Seasons.Add(new BlueSeason()
        {
            StartDate = new DateOnly(currentYear, 6, 1),
            EndDate = new DateOnly(currentYear + 1, 5, 31),
        });

        _context.AppSettings.Add(new BlueAppSettings()
        {
            CurrentSeasonId = seasonResult.Entity.Id
        });

        await _context.SaveChangesAsync();
    }

    private static void VerifyResult(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }
        
        throw new Exception(result.Errors.First().Description);
    }
}