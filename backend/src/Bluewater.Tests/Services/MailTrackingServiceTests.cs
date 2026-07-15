using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Mail;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MailTrackingServiceTests : SqliteServiceTestBase
{
    private readonly IMailTrackingService _sut;

    public MailTrackingServiceTests()
    {
        _sut = GetService<IMailTrackingService>();
    }

    [Fact]
    public async Task RecordClickAsync_ReturnsOriginalUrl_AndIncrementsClickCount()
    {
        var mailing = new Mailing { Id = Guid.NewGuid(), Subject = "S", BodyMarkdown = "B", SenderKey = "default" };
        var recipient = new MailingRecipient { Id = Guid.NewGuid(), MailingId = mailing.Id, Email = "a@example.com", FullName = "A", TrackingToken = "rt" };
        var link = new MailingRecipientLink { Id = Guid.NewGuid(), MailingRecipientId = recipient.Id, OriginalUrl = "https://example.com/page", Token = "lt" };
        Db.Mailings.Add(mailing);
        Db.MailingRecipients.Add(recipient);
        Db.MailingRecipientLinks.Add(link);
        await Db.SaveChangesAsync();

        var url = await _sut.RecordClickAsync("lt");
        await _sut.RecordClickAsync("lt");

        url.ShouldBe("https://example.com/page");
        var updated = Db.MailingRecipientLinks.Single(x => x.Token == "lt");
        updated.ClickCount.ShouldBe(2);
        updated.FirstClickedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordClickAsync_UnknownToken_ReturnsNull()
    {
        var url = await _sut.RecordClickAsync("does-not-exist");

        url.ShouldBeNull();
    }

    [Fact]
    public async Task RecordOpenAsync_UnknownToken_NoOps()
    {
        await _sut.RecordOpenAsync("does-not-exist");
        // no exception, no rows affected — nothing to assert against
    }

    [Fact]
    public async Task RecordOpenAsync_KnownToken_MarksOpened_AndIncrementsOpenCount()
    {
        var mailing = new Mailing { Id = Guid.NewGuid(), Subject = "S", BodyMarkdown = "B", SenderKey = "default" };
        var recipient = new MailingRecipient { Id = Guid.NewGuid(), MailingId = mailing.Id, Email = "a@example.com", FullName = "A", TrackingToken = "rt" };
        Db.Mailings.Add(mailing);
        Db.MailingRecipients.Add(recipient);
        await Db.SaveChangesAsync();

        await _sut.RecordOpenAsync("rt");
        await _sut.RecordOpenAsync("rt");

        var updated = Db.MailingRecipients.Single(x => x.TrackingToken == "rt");
        updated.Opened.ShouldBeTrue();
        updated.OpenCount.ShouldBe(2);
        updated.FirstOpenedAt.ShouldNotBeNull();
    }
}
