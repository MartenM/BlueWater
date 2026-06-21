using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.News;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bluewater.Infra.Context;

public class BluewaterContextSeeder
{
    private static readonly string SeedImagesPath = Path.Combine(AppContext.BaseDirectory, "Seeding", "Data", "Images");

    public BluewaterContextSeeder(BluewaterContext context, UserManager<BlueUser> userManager, IFileStorageService fileStorageService, ILogger<BluewaterContextSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    private BluewaterContext _context;
    private UserManager<BlueUser> _userManager;
    private IFileStorageService _fileStorageService;
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

        using (var profilePicture = new MemoryStream(LoadSeedImage("example-user-profile.png")))
        {
            var storedFile = await _fileStorageService.StoreAsync(profilePicture, "example-user-profile.png", "image/png");
            adminUser.ProfilePictureFileId = storedFile.Id;
        }

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

        var gyasIcon = await CreateNewsIconAsync("Gyas", "gyas-logo.png");
        var flagIcon = await CreateNewsIconAsync("Vlag", "flag.png");
        var bowaIcon = await CreateNewsIconAsync("Bowa", "bowa.png");

        _context.NewsPosts.AddRange(
            new NewsPost()
            {
                Id = Guid.NewGuid(),
                Title = "Open dag: kom roeien bij Gyas",
                ShortText = "Nieuw in Groningen of altijd al willen roeien? Op onze open dag laten we je kennismaken met de boot, de loods en de club.",
                AdditionalText = "Iedereen is welkom, ervaring is niet nodig. Loop binnen tussen 10:00 en 16:00 uur, dan staat er een boot voor je klaar en is er koffie en thee in de loods.",
                MembersOnly = false,
                IconId = gyasIcon.Id
            },
            new NewsPost()
            {
                Id = Guid.NewGuid(),
                Title = "Vlag uit voor de Varsity",
                ShortText = "Onze eerste acht heeft de Varsity gewonnen! Een prachtige prestatie na een seizoen vol hard trainen.",
                AdditionalText = "De wedstrijd werd verroeid op de Bosbaan, waar onze ploeg een ruime voorsprong nam in de laatste 500 meter. Felicitaties aan iedereen die heeft bijgedragen aan dit resultaat.",
                MembersOnly = false,
                IconId = flagIcon.Id
            },
            new NewsPost()
            {
                Id = Guid.NewGuid(),
                Title = "Nieuwe samenwerking met Bowa",
                ShortText = "Gyas en Bowa slaan de handen ineen voor onderhoud aan de botenloods en materiaal.",
                AdditionalText = "Dankzij deze samenwerking kunnen we onze boten en loods het komende seizoen op niveau houden. Meer details volgen tijdens de eerstvolgende ledenvergadering.",
                MembersOnly = false,
                IconId = bowaIcon.Id
            },
            new NewsPost()
            {
                Id = Guid.NewGuid(),
                Title = "Agenda algemene ledenvergadering",
                ShortText = "De agenda voor de komende ALV staat online. Leden kunnen deze inzien in het ledenportaal.",
                AdditionalText = "Onderwerpen op de agenda zijn onder andere de begroting voor het nieuwe seizoen en de verkiezing van twee nieuwe bestuursleden.",
                MembersOnly = true,
                IconId = gyasIcon.Id
            }
        );

        await _context.SaveChangesAsync();
    }

    private async Task<NewsIcon> CreateNewsIconAsync(string name, string fileName)
    {
        using var content = new MemoryStream(LoadSeedImage(fileName));
        var storedFile = await _fileStorageService.StoreAsync(content, fileName, "image/png");

        var icon = new NewsIcon()
        {
            Id = Guid.NewGuid(),
            Name = name,
            FileId = storedFile.Id
        };

        _context.NewsIcons.Add(icon);
        return icon;
    }

    private static byte[] LoadSeedImage(string fileName) =>
        File.ReadAllBytes(Path.Combine(SeedImagesPath, fileName));

    private static void VerifyResult(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new Exception(result.Errors.First().Description);
    }
}