using Bluewater.Domain.Models.Groups;
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

        await seeder.SeedDevelopmentAsync();

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
    public async Task SeedAsync_CreatesDefaultMailLayoutAndTemplate()
    {
        var seeder = GetService<BluewaterContextSeeder>();

        await seeder.SeedDevelopmentAsync();

        var layout = await Db.MailLayouts.SingleAsync(x => x.Name == "default");
        layout.IsDefault.ShouldBeTrue();
        layout.HeaderHtml.ShouldContain("Logo_Gyas_Totaal.svg");
        layout.FooterHtml.ShouldContain("{{AddressBlock}}");

        var template = await Db.MailTemplates.SingleAsync(x => x.Name == "default");
        template.Kind.ShouldBe(Domain.Models.Mail.MailTemplateKind.Mailing);
        template.DefaultLayoutId.ShouldBe(layout.Id);
        template.BodyMarkdown.ShouldContain("{{FirstName}}");
    }

    [Fact]
    public async Task SeedAsync_CreatesRequiredTransactionalMailTemplates()
    {
        var seeder = GetService<BluewaterContextSeeder>();

        await seeder.SeedDevelopmentAsync();

        var welcome = await Db.MailTemplates.SingleAsync(
            x => x.Name == Domain.Models.Mail.TransactionalMailTemplateNames.WelcomeEmail);
        welcome.Kind.ShouldBe(Domain.Models.Mail.MailTemplateKind.Transactional);

        var defaultLayout = await Db.MailLayouts.SingleAsync(x => x.Name == "default");
        welcome.DefaultLayoutId.ShouldBe(defaultLayout.Id);
    }

    [Fact]
    public async Task SeedAsync_DoesNotDuplicate_RequiredTransactionalMailTemplates_OnSubsequentStartup()
    {
        var seeder = GetService<BluewaterContextSeeder>();
        await seeder.SeedDevelopmentAsync();

        // Simulate a re-run against an existing DB (the "Users already exist" path).
        await seeder.SeedDevelopmentAsync();

        var count = await Db.MailTemplates.CountAsync(
            x => x.Name == Domain.Models.Mail.TransactionalMailTemplateNames.WelcomeEmail);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwrite_ManuallyEditedTransactionalTemplateContent()
    {
        var seeder = GetService<BluewaterContextSeeder>();
        await seeder.SeedDevelopmentAsync();

        var welcome = await Db.MailTemplates.SingleAsync(
            x => x.Name == Domain.Models.Mail.TransactionalMailTemplateNames.WelcomeEmail);
        welcome.SubjectTemplate = "Admin-edited subject";
        await Db.SaveChangesAsync();

        await seeder.SeedDevelopmentAsync();

        var reloaded = await Db.MailTemplates.SingleAsync(
            x => x.Name == Domain.Models.Mail.TransactionalMailTemplateNames.WelcomeEmail);
        reloaded.SubjectTemplate.ShouldBe("Admin-edited subject");
    }

    [Fact]
    public async Task SeedAsync_DoesNothing_WhenUsersAlreadyExist()
    {
        await CreateUserAsync();

        var seeder = GetService<BluewaterContextSeeder>();
        await seeder.SeedDevelopmentAsync();

        (await Db.Seasons.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task SeedProductionAsync_CreatesMinimalEnvironment()
    {
        var seeder = GetService<BluewaterContextSeeder>();

        await seeder.SeedProductionAsync();

        var admin = await Db.Users.SingleAsync(x => x.UserName == "admin");

        var category = await Db.UserGroupCategories.SingleAsync();
        category.Name.ShouldBe("General");

        var group = await Db.UserGroups
            .Include(g => g.Permissions)
            .SingleAsync();
        group.Name.ShouldBe("Administrators");
        group.UserGroupCategoryId.ShouldBe(category.Id);
        group.Permissions.Select(p => p.Permission).ToHashSet()
            .ShouldBe(Enum.GetValues<BluePermission>().ToHashSet());

        var season = await Db.Seasons.SingleAsync();

        var instance = await Db.UserGroupInstances
            .Include(i => i.Members)
            .SingleAsync();
        instance.UserGroupId.ShouldBe(group.Id);
        instance.SeasonId.ShouldBe(season.Id);
        instance.Members.ShouldHaveSingleItem().UserId.ShouldBe(admin.Id);

        var news = await Db.NewsPosts.SingleAsync();
        news.IconId.ShouldBeNull();
    }
}
