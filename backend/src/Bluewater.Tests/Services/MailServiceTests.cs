using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Mail;
using Bluewater.Tests.TestSupport;
using FluentValidation;

namespace Bluewater.Tests.Services;

public class MailServiceTests : SqliteServiceTestBase
{
    private readonly IMailService _sut;

    public MailServiceTests()
    {
        _sut = GetService<IMailService>();
    }

    [Fact]
    public async Task SendTransactionalAsync_EnqueuesOneJobPerRecipient_WithRenderedContent()
    {
        // Transactional templates can no longer be created via the service (they're seeded), so
        // insert directly through the DbContext to set up this fixture.
        var template = new MailTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Welcome",
            Kind = MailTemplateKind.Transactional,
            SubjectTemplate = "Hi {{FirstName}}",
            BodyMarkdown = "Hello **{{FullName}}**!",
            DefaultSenderKey = "default",
        };
        Db.MailTemplates.Add(template);
        await Db.SaveChangesAsync();

        var request = new SendTransactionalMailRequest(
            TemplateId: template.Id,
            SubjectOverride: null,
            BodyMarkdownOverride: null,
            SenderKey: "default",
            Recipients:
            [
                new TransactionalRecipient("a@example.com", "A", new Dictionary<string, string> { ["FirstName"] = "Alice", ["FullName"] = "Alice A" }, null),
                new TransactionalRecipient("b@example.com", "B", new Dictionary<string, string> { ["FirstName"] = "Bob", ["FullName"] = "Bob B" }, null),
            ],
            Cc: [],
            Bcc: [],
            ReplyToOverride: null,
            AttachmentStoredFileIds: []);

        await _sut.SendTransactionalAsync(request);

        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(2);
        var firstArgs = BackgroundJobClient.EnqueuedJobs[0].Args;
        firstArgs[0].ShouldBe("default");
        ((List<string>)firstArgs[1]!)[0].ShouldBe("a@example.com");
        firstArgs[5].ShouldBe("Hi Alice");
        ((string)firstArgs[6]!).ShouldContain("<strong>Alice A</strong>");
    }

    [Fact]
    public async Task SendTransactionalAsync_UsesOverrideContent_WhenNoTemplateId()
    {
        var request = new SendTransactionalMailRequest(
            TemplateId: null,
            SubjectOverride: "Override subject",
            BodyMarkdownOverride: "Override **body**",
            SenderKey: "default",
            Recipients: [new TransactionalRecipient("a@example.com", null, new Dictionary<string, string>(), null)],
            Cc: [],
            Bcc: [],
            ReplyToOverride: null,
            AttachmentStoredFileIds: []);

        await _sut.SendTransactionalAsync(request);

        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(1);
        var args = BackgroundJobClient.EnqueuedJobs[0].Args;
        args[5].ShouldBe("Override subject");
        ((string)args[6]!).ShouldContain("<strong>body</strong>");
    }

    [Fact]
    public async Task SendTransactionalAsync_Throws_WhenNoRecipients()
    {
        var request = new SendTransactionalMailRequest(
            TemplateId: null, SubjectOverride: "S", BodyMarkdownOverride: "B", SenderKey: "default",
            Recipients: [], Cc: [], Bcc: [], ReplyToOverride: null, AttachmentStoredFileIds: []);

        await Should.ThrowAsync<ValidationException>(() => _sut.SendTransactionalAsync(request));
    }

    [Fact]
    public async Task SendTransactionalAsync_Throws_WhenBothTemplateIdAndOverrideMissing()
    {
        var request = new SendTransactionalMailRequest(
            TemplateId: null, SubjectOverride: null, BodyMarkdownOverride: null, SenderKey: "default",
            Recipients: [new TransactionalRecipient("a@example.com", null, new Dictionary<string, string>(), null)],
            Cc: [], Bcc: [], ReplyToOverride: null, AttachmentStoredFileIds: []);

        await Should.ThrowAsync<ValidationException>(() => _sut.SendTransactionalAsync(request));
    }

    [Fact]
    public async Task SendTransactionalAsync_Throws_WhenTemplateDoesNotExist()
    {
        var request = new SendTransactionalMailRequest(
            TemplateId: Guid.NewGuid(), SubjectOverride: null, BodyMarkdownOverride: null, SenderKey: "default",
            Recipients: [new TransactionalRecipient("a@example.com", null, new Dictionary<string, string>(), null)],
            Cc: [], Bcc: [], ReplyToOverride: null, AttachmentStoredFileIds: []);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.SendTransactionalAsync(request));
    }

    [Fact]
    public async Task SendTransactionalAsync_MergesUserTokens_WhenUserIdProvided()
    {
        var user = await CreateUserAsync("carol", "carol@example.com");

        var request = new SendTransactionalMailRequest(
            TemplateId: null,
            SubjectOverride: "Hi {{FirstName}}",
            BodyMarkdownOverride: "Body",
            SenderKey: "default",
            Recipients: [new TransactionalRecipient(user.Email!, null, new Dictionary<string, string> { ["FirstName"] = "Carol" }, user.Id)],
            Cc: [],
            Bcc: [],
            ReplyToOverride: null,
            AttachmentStoredFileIds: []);

        await _sut.SendTransactionalAsync(request);

        var args = BackgroundJobClient.EnqueuedJobs[0].Args;
        args[5].ShouldBe("Hi Carol");
    }
}
