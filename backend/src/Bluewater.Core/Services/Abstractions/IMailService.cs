using Bluewater.Core.Dto.Mail;

namespace Bluewater.Core.Services.Abstractions;

public interface IMailService
{
    Task SendTransactionalAsync(SendTransactionalMailRequest request);
}
