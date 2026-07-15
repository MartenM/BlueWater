using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Mail;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/mail/senders")]
[BlueAuthorize(BluePermission.AdminModifyMailings)]
public class MailSendersController : ControllerBase
{
    private readonly MailOptions _mailOptions;

    public MailSendersController(IOptions<MailOptions> mailOptions)
    {
        _mailOptions = mailOptions.Value;
    }

    [HttpGet]
    public Task<List<MailSenderInfoDto>> List()
    {
        var senders = _mailOptions
            .Senders.Select(s => new MailSenderInfoDto { Key = s.Key, DisplayName = s.DisplayName })
            .ToList();
        return Task.FromResult(senders);
    }
}
