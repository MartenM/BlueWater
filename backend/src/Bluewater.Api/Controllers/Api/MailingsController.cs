using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/mailings")]
[BlueAuthorize(BluePermission.AdminModifyMailings)]
public class MailingsController : ControllerBase
{
    private readonly IMailingService _service;

    public MailingsController(IMailingService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<MailingDto>> List()
    {
        return _service.ListAsync();
    }

    [HttpGet("{id:guid}")]
    public Task<MailingDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    [HttpPost]
    public Task<MailingDto> Create(UpsertMailingRequest request)
    {
        return _service.CreateAsync(request);
    }

    [HttpPut("{id:guid}")]
    public Task<MailingDto> Update(Guid id, UpsertMailingRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    [HttpPost("{id:guid}/targets/clusters/{clusterId:guid}")]
    public Task<MailingDto> AddTargetCluster(Guid id, Guid clusterId)
    {
        return _service.AddTargetClusterAsync(id, clusterId);
    }

    [HttpDelete("{id:guid}/targets/clusters/{clusterId:guid}")]
    public Task RemoveTargetCluster(Guid id, Guid clusterId)
    {
        return _service.RemoveTargetClusterAsync(id, clusterId);
    }

    [HttpPost("{id:guid}/targets/groupinstances/{instanceId:guid}")]
    public Task<MailingDto> AddTargetGroupInstance(Guid id, Guid instanceId)
    {
        return _service.AddTargetGroupInstanceAsync(id, instanceId);
    }

    [HttpDelete("{id:guid}/targets/groupinstances/{instanceId:guid}")]
    public Task RemoveTargetGroupInstance(Guid id, Guid instanceId)
    {
        return _service.RemoveTargetGroupInstanceAsync(id, instanceId);
    }

    [HttpPost("{id:guid}/preview", Name = "MailingsPreview")]
    public Task<MailingPreviewDto> Preview(Guid id)
    {
        return _service.PreviewAsync(id);
    }

    [HttpPost("{id:guid}/proof")]
    public Task SendProof(Guid id)
    {
        return _service.SendProofAsync(id);
    }

    [HttpPost("{id:guid}/send")]
    public Task Send(Guid id)
    {
        return _service.SendAsync(id);
    }

    [HttpGet("{id:guid}/stats")]
    public Task<MailingStatsDto> Stats(Guid id)
    {
        return _service.GetStatsAsync(id);
    }

    [HttpGet("{id:guid}/target-count")]
    public Task<int> TargetCount(Guid id)
    {
        return _service.GetResolvedTargetCountAsync(id);
    }

    [HttpGet("{id:guid}/recipients")]
    public Task<PagedResult<MailingRecipientDto>> Recipients(Guid id, int page = 1, int pageSize = 50)
    {
        return _service.GetRecipientsAsync(id, page, pageSize);
    }
}
