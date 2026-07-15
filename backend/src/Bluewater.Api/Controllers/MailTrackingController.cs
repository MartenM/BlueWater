using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bluewater.Api.Controllers;

/// <summary>
/// Anonymous click/open tracking endpoints embedded into mailing HTML bodies at send time.
/// Unknown tokens no-op / redirect to a safe fallback rather than surfacing an error, since
/// this is a public, unauthenticated surface. State mutation lives in IMailTrackingService;
/// this controller only translates its result into a redirect/binary response.
/// </summary>
[ApiController]
[Route("api/mail")]
public class MailTrackingController : ControllerBase
{
    private static readonly byte[] TransparentGif =
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B,
    ];

    private readonly IMailTrackingService _trackingService;
    private readonly MailOptions _mailOptions;

    public MailTrackingController(IMailTrackingService trackingService, IOptions<MailOptions> mailOptions)
    {
        _trackingService = trackingService;
        _mailOptions = mailOptions.Value;
    }

    [HttpGet("r/{token}")]
    public async Task<IActionResult> Click(string token)
    {
        var originalUrl = await _trackingService.RecordClickAsync(token);
        return Redirect(originalUrl ?? _mailOptions.PublicBaseUrl ?? "/");
    }

    [HttpGet("p/{token}.gif")]
    public async Task<IActionResult> Open(string token)
    {
        await _trackingService.RecordOpenAsync(token);
        return File(TransparentGif, "image/gif");
    }
}
