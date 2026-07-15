using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Services.Abstractions;

namespace Bluewater.Tests.TestSupport;

/// <summary>
/// Hand-rolled fake for <see cref="IMailTransportService"/> — the real implementation talks to a
/// live SMTP server, which the SQLite test base doesn't have. Lets tests control the outcome of
/// SendAsync (success, permanent bounce, or a generic transient failure) to assert how callers
/// (e.g. MailingRecipientSendJob) react.
/// </summary>
public class FakeMailTransportService : IMailTransportService
{
    public List<MailMessageEnvelope> SentEnvelopes { get; } = [];

    /// <summary>When set, SendAsync throws this instead of recording the envelope as sent.</summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task SendAsync(MailMessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        SentEnvelopes.Add(envelope);
        return Task.CompletedTask;
    }
}
