using System.IO.Compression;
using System.Text;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bluewater.Infra.Context;

public class BluewaterContextSeeder
{
    private const int ProfilePictureWidth = 75;
    private const int ProfilePictureHeight = 100;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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

        using (var placeholderPicture = new MemoryStream(CreatePlaceholderProfilePicturePng()))
        {
            var storedFile = await _fileStorageService.StoreAsync(placeholderPicture, "placeholder.png", "image/png");
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

    /// <summary>
    /// Builds a solid-gray PNG matching the dimensions UserProfileService requires, encoded by hand
    /// (no SixLabors.ImageSharp/System.Drawing dependency) for the same licensing reasons that
    /// UserProfileService parses PNG/JPEG headers manually.
    /// </summary>
    private static byte[] CreatePlaceholderProfilePicturePng()
    {
        const byte gray = 200;

        using var raw = new MemoryStream();
        for (var y = 0; y < ProfilePictureHeight; y++)
        {
            raw.WriteByte(0); // filter: none
            for (var x = 0; x < ProfilePictureWidth; x++)
            {
                raw.WriteByte(gray);
                raw.WriteByte(gray);
                raw.WriteByte(gray);
            }
        }

        using var idat = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionLevel.Optimal, leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(zlib);
        }

        using var png = new MemoryStream();
        png.Write(PngSignature);
        WriteChunk(png, "IHDR", BuildIhdr(ProfilePictureWidth, ProfilePictureHeight));
        WriteChunk(png, "IDAT", idat.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var ihdr = new byte[13];
        WriteUInt32BigEndian(ihdr, 0, (uint)width);
        WriteUInt32BigEndian(ihdr, 4, (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 2; // color type: truecolor
        ihdr[10] = 0; // compression method
        ihdr[11] = 0; // filter method
        ihdr[12] = 0; // interlace method
        return ihdr;
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);

        var length = new byte[4];
        WriteUInt32BigEndian(length, 0, (uint)data.Length);
        output.Write(length);
        output.Write(typeBytes);
        output.Write(data);

        var crc = new byte[4];
        WriteUInt32BigEndian(crc, 0, Crc32(typeBytes, data));
        output.Write(crc);
    }

    private static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Crc32(byte[] typeBytes, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in typeBytes)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        foreach (var b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            table[i] = c;
        }
        return table;
    }
}