using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[BlueAuthorize(BluePermission.AdminModifyMailTemplates)]
public class MailLayoutsController : ControllerBase
{
    private readonly IMailLayoutService _service;

    public MailLayoutsController(IMailLayoutService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<MailLayoutDto>> List()
    {
        return _service.ListAsync();
    }

    [HttpGet("{id:guid}")]
    public Task<MailLayoutDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    [HttpPost]
    public Task<MailLayoutDto> Create(UpsertMailLayoutRequest request)
    {
        return _service.CreateAsync(request);
    }

    [HttpPut("{id:guid}")]
    public Task<MailLayoutDto> Update(Guid id, UpsertMailLayoutRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
