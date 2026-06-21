using Bluewater.Infra.Context;
using Bluewater.Infra.Services.Abstractions;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Context;

public class BluewaterContextSeederTests : SqliteServiceTestBase
{
    [Fact]
    public async Task SeedAsync_AssignsPlaceholderProfilePicture_ToAdminUser()
    {
        var seeder = GetService<BluewaterContextSeeder>();

        await seeder.SeedAsync();

        var admin = await Db.Users.FirstAsync(x => x.UserName == "admin");
        admin.ProfilePictureFileId.ShouldNotBeNull();

        var fileStorageService = GetService<IFileStorageService>();
        var (metadata, content) = await fileStorageService.RetrieveAsync(admin.ProfilePictureFileId!.Value);
        await using var _ = content;
        metadata.ContentType.ShouldBe("image/png");

        using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered);
        var bytes = buffered.ToArray();

        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        bytes.AsSpan(0, 8).SequenceEqual(pngSignature).ShouldBeTrue();

        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        width.ShouldBe(75);
        height.ShouldBe(100);
    }

    [Fact]
    public async Task SeedAsync_DoesNothing_WhenUsersAlreadyExist()
    {
        await CreateUserAsync();

        var seeder = GetService<BluewaterContextSeeder>();
        await seeder.SeedAsync();

        (await Db.Seasons.CountAsync()).ShouldBe(0);
    }
}
