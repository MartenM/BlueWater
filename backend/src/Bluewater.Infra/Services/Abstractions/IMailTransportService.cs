using Bluewater.Domain.Models.Mail;

namespace Bluewater.Infra.Services.Abstractions;

public interface IMailTransportService
{
    Task SendAsync(MailMessageEnvelope envelope, CancellationToken cancellationToken = default);
}
