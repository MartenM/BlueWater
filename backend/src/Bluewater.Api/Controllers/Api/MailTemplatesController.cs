using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Mail;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[BlueAuthorize(BluePermission.AdminModifyMailTemplates)]
public class MailTemplatesController : ControllerBase
{
    private readonly IMailTemplateService _service;

    public MailTemplatesController(IMailTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<MailTemplateDto>> List(MailTemplateKind? kind = null)
    {
        return _service.ListAsync(kind);
    }

    [HttpGet("{id:guid}")]
    public Task<MailTemplateDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    [HttpGet("placeholders")]
    public Task<List<MailPlaceholderDto>> GetPlaceholders([FromQuery] Guid? templateId = null)
    {
        return _service.GetPlaceholdersAsync(templateId);
    }

    [HttpPost]
    public Task<MailTemplateDto> Create(UpsertMailTemplateRequest request)
    {
        return _service.CreateAsync(request);
    }

    [HttpPut("{id:guid}")]
    public Task<MailTemplateDto> Update(Guid id, UpsertMailTemplateRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    [HttpPost("{id:guid}/preview")]
    public Task<MailTemplatePreviewDto> Preview(Guid id, MailTemplatePreviewRequest request)
    {
        return _service.PreviewAsync(id, request);
    }
}
