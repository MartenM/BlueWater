using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
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
            Firstname = "Admin",
            SurnamePrefix = "der",
            Surname = "Localhost",
        }, "admin");
        VerifyResult(res);

        // Create season
        var currentYear = DateTime.Now.Year;
        
        var adminUser = await _userManager.FindByNameAsync("admin") ?? throw new Exception("Admin user not found");

        var seasonResult = _context.Seasons.Add(new BlueSeason()
        {
            StartDate = new DateOnly(currentYear, 6, 1),
            EndDate = new DateOnly(currentYear + 1, 5, 31),
        });

        var settingsResult = _context.AppSettings.Add(new BlueAppSettings()
        {
            CurrentSeasonId = seasonResult.Entity.Id
        });

        var groupCategoryResults = _context.UserGroupCategories.Add(new UserGroupCategory()
        {
            Name = "General"
        });

        var membersGroup = _context.UserGroups.Add(new UserGroup()
        {
            Name = $"Members",
            Description = "Active members.",
            UserGroupCategoryId =  groupCategoryResults.Entity.Id,
        });
        
        
        var maintainerGroup = _context.UserGroups.Add(new UserGroup()
        {
            Name = $"Maintainers",
            Description = "Maintainers that have access to all site functionalities.",
            UserGroupCategoryId =  groupCategoryResults.Entity.Id,
        });

        var membersGroupInstance = _context.UserGroupInstances.Add(new UserGroupInstance()
        {
            UserGroupId = membersGroup.Entity.Id,
            SeasonId = seasonResult.Entity.Id,
            Members = new List<UserGroupInstanceMember>()
            {
                new UserGroupInstanceMember()
                {
                    UserId = adminUser.Id
                }
            }
        });

        var maintainerGroupInstance = _context.UserGroupInstances.Add(new UserGroupInstance()
        {
            UserGroupId = maintainerGroup.Entity.Id,
            SeasonId = seasonResult.Entity.Id,
            Members = new List<UserGroupInstanceMember>()
            {
                new UserGroupInstanceMember()
                {
                    UserId = adminUser.Id
                }
            },
            Permissions = Enum.GetValues<BluePermission>().Select(p => new UserGroupInstancePermission()
            {
                Permission = p
            }).ToList()
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