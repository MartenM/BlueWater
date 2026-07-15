using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Mail;
using FluentValidation;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MailTemplateServiceTests : SqliteServiceTestBase
{
    private readonly IMailTemplateService _sut;
    private readonly IMailLayoutService _layoutService;

    public MailTemplateServiceTests()
    {
        _sut = GetService<IMailTemplateService>();
        _layoutService = GetService<IMailLayoutService>();
    }

    [Fact]
    public async Task CreateAsync_ReturnsTemplate()
    {
        var result = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Newsletter", MailTemplateKind.Mailing, "Hi {{FirstName}}", "Welcome, **{{FullName}}**!", null, "default"));

        result.Name.ShouldBe("Newsletter");
        result.Kind.ShouldBe(MailTemplateKind.Mailing);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenKindIsTransactional()
    {
        await Should.ThrowAsync<BlueValidationException>(() => _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Some Transactional Template", MailTemplateKind.Transactional, "S", "B", null, null)));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var template = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Old", MailTemplateKind.Mailing, "Old subject", "Old body", null, null));

        var result = await _sut.UpdateAsync(template.Id, new UpsertMailTemplateRequest(
            "New", MailTemplateKind.Mailing, "New subject", "New body", null, null));

        result.Name.ShouldBe("New");
        result.SubjectTemplate.ShouldBe("New subject");
        result.BodyMarkdown.ShouldBe("New body");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenKindChanges()
    {
        var template = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Mailing1", MailTemplateKind.Mailing, "S", "B", null, null));

        await Should.ThrowAsync<BlueValidationException>(() => _sut.UpdateAsync(template.Id, new UpsertMailTemplateRequest(
            "Mailing1", MailTemplateKind.Transactional, "S", "B", null, null)));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenRenamingTransactionalTemplate()
    {
        var template = SeedTransactionalTemplate("Locked Name");

        await Should.ThrowAsync<BlueValidationException>(() => _sut.UpdateAsync(template.Id, new UpsertMailTemplateRequest(
            "New Name", MailTemplateKind.Transactional, template.SubjectTemplate, template.BodyMarkdown, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_AllowsEditingContent_OfTransactionalTemplate()
    {
        var template = SeedTransactionalTemplate("Locked Name");

        var result = await _sut.UpdateAsync(template.Id, new UpsertMailTemplateRequest(
            "Locked Name", MailTemplateKind.Transactional, "New subject", "New body", null, "default"));

        result.Name.ShouldBe("Locked Name");
        result.SubjectTemplate.ShouldBe("New subject");
        result.BodyMarkdown.ShouldBe("New body");
        result.DefaultSenderKey.ShouldBe("default");
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenTemplateIsTransactional()
    {
        var template = SeedTransactionalTemplate("Locked Name");

        await Should.ThrowAsync<BlueValidationException>(() => _sut.DeleteAsync(template.Id));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenNotFound()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTemplate()
    {
        var template = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Temp", MailTemplateKind.Mailing, "S", "B", null, null));

        await _sut.DeleteAsync(template.Id);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(template.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDefaultLayoutIdDoesNotExist()
    {
        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Bad", MailTemplateKind.Mailing, "S", "B", Guid.NewGuid(), null)));
    }

    [Fact]
    public async Task ListAsync_FiltersByKind()
    {
        await _sut.CreateAsync(new UpsertMailTemplateRequest("Mailing1", MailTemplateKind.Mailing, "S", "B", null, null));
        SeedTransactionalTemplate("Trans1");

        var result = await _sut.ListAsync(MailTemplateKind.Transactional);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Trans1");
    }

    [Fact]
    public async Task PreviewAsync_RendersSampleData_AndBoldsFullName()
    {
        var layout = await _layoutService.CreateAsync(new UpsertMailLayoutRequest("L", "<div>header</div>", "<div>footer</div>", true));
        var template = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Welcome", MailTemplateKind.Mailing, "Hi {{FirstName}}", "Hello **{{FullName}}**!", layout.Id, null));

        var preview = await _sut.PreviewAsync(template.Id, new MailTemplatePreviewRequest(null, null, null));

        preview.Subject.ShouldBe("Hi Jane");
        preview.HtmlBody.ShouldContain("<strong>Jane Doe</strong>");
        preview.HtmlBody.ShouldContain("header");
        preview.HtmlBody.ShouldContain("footer");
        preview.PlainTextBody.ShouldContain("Jane Doe");
    }

    [Fact]
    public async Task GetPlaceholdersAsync_ReturnsBaseTokens_WhenNoTemplateId()
    {
        var result = await _sut.GetPlaceholdersAsync(null);

        result.ShouldContain(x => x.Token == "FirstName");
        result.ShouldContain(x => x.Token == "FullName");
        result.ShouldContain(x => x.Token == "Email");
        result.ShouldContain(x => x.Token == "FormalSalutation");
        result.ShouldContain(x => x.Token == "AddressBlock");
        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetPlaceholdersAsync_ReturnsBaseTokensOnly_ForMailingTemplate()
    {
        var template = await _sut.CreateAsync(new UpsertMailTemplateRequest(
            "Mailing1", MailTemplateKind.Mailing, "S", "B", null, null));

        var result = await _sut.GetPlaceholdersAsync(template.Id);

        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetPlaceholdersAsync_Throws_WhenTemplateNotFound()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetPlaceholdersAsync(Guid.NewGuid()));
    }

    private MailTemplate SeedTransactionalTemplate(string name)
    {
        var template = new MailTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Kind = MailTemplateKind.Transactional,
            SubjectTemplate = "S",
            BodyMarkdown = "B",
        };
        Db.MailTemplates.Add(template);
        Db.SaveChanges();
        return template;
    }
}
