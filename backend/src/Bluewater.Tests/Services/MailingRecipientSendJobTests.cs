using Bluewater.Core.Services.Mail;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Exceptions;
using Bluewater.Tests.TestSupport;
using Shouldly;

namespace Bluewater.Tests.Services;

public class MailingRecipientSendJobTests : SqliteServiceTestBase
{
    private async Task<MailingRecipient> CreateRecipientAsync(Guid? userId = null)
    {
        var mailing = new Mailing
        {
            Id = Guid.NewGuid(),
            Subject = "Test subject",
            BodyMarkdown = "Body",
            SenderKey = "default",
            Status = MailingStatus.Sending,
        };
        Db.Mailings.Add(mailing);

        var recipient = new MailingRecipient
        {
            Id = Guid.NewGuid(),
            MailingId = mailing.Id,
            UserId = userId,
            Email = "recipient@example.com",
            FullName = "Test Recipient",
            RenderedSubject = "Test subject",
            RenderedHtmlBody = "<p>Body</p>",
            TrackingToken = Guid.NewGuid().ToString("N"),
        };
        Db.MailingRecipients.Add(recipient);
        await Db.SaveChangesAsync();

        return recipient;
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_MarksRecipientSent()
    {
        var recipient = await CreateRecipientAsync();
        var sut = GetService<MailingRecipientSendJob>();

        await sut.ExecuteAsync(recipient.Id);

        var updated = await Db.MailingRecipients.FindAsync(recipient.Id);
        updated!.Sent.ShouldBeTrue();
        updated.SentAt.ShouldNotBeNull();
        updated.Bounced.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_OnPermanentRejection_MarksBouncedAndUnconfirmsEmail()
    {
        var user = await CreateUserAsync();
        user.EmailConfirmed.ShouldBeTrue();
        var recipient = await CreateRecipientAsync(user.Id);

        MailTransportService.ExceptionToThrow = new MailRecipientRejectedException("Recipient(s) recipient@example.com rejected: 550 mailbox unavailable");

        var sut = GetService<MailingRecipientSendJob>();
        await sut.ExecuteAsync(recipient.Id);

        var updatedRecipient = await Db.MailingRecipients.FindAsync(recipient.Id);
        updatedRecipient!.Bounced.ShouldBeTrue();
        updatedRecipient.BounceReason.ShouldNotBeNullOrEmpty();
        updatedRecipient.FailedAt.ShouldNotBeNull();
        updatedRecipient.Sent.ShouldBeFalse();

        var updatedUser = await Db.Users.FindAsync(user.Id);
        updatedUser!.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_OnTransientFailure_PropagatesWithoutMarkingBounced()
    {
        // Mirrors what MailTransportService does for a non-550 SmtpCommandException (e.g. 421/450):
        // it rethrows unchanged rather than throwing MailRecipientRejectedException, so this job
        // must not treat it as a bounce.
        var user = await CreateUserAsync();
        var recipient = await CreateRecipientAsync(user.Id);

        MailTransportService.ExceptionToThrow = new InvalidOperationException("450 mailbox temporarily unavailable");

        var sut = GetService<MailingRecipientSendJob>();
        await Should.ThrowAsync<InvalidOperationException>(() => sut.ExecuteAsync(recipient.Id));

        var updatedRecipient = await Db.MailingRecipients.FindAsync(recipient.Id);
        updatedRecipient!.Bounced.ShouldBeFalse();
        updatedRecipient.Sent.ShouldBeFalse();
        updatedRecipient.FailedAt.ShouldBeNull();

        var updatedUser = await Db.Users.FindAsync(user.Id);
        updatedUser!.EmailConfirmed.ShouldBeTrue();
    }
}
