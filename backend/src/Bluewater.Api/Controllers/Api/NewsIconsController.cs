using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.News;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for the reusable news icon catalog. Listing/uploading/deleting are permission-gated;
/// streaming an icon's image content is open so public news posts can render their icon for
/// anonymous visitors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NewsIconsController : ControllerBase
{
    private readonly INewsIconService _service;

    public NewsIconsController(INewsIconService service)
    {
        _service = service;
    }

    /// <summary>Lists the active news icons available for selection.</summary>
    [BlueAuthorize(BluePermission.NewsModify)]
    [HttpGet]
    public Task<List<NewsIconDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Streams a news icon's image content, e.g. for use as an &lt;img src&gt;.</summary>
    [HttpGet("{id:guid}/content")]
    [Produces("application/octet-stream")]
    public async Task<FileResult> GetContent(Guid id)
    {
        var (metadata, content) = await _service.GetContentAsync(id);
        return File(content, metadata.ContentType, metadata.OriginalFileName);
    }

    /// <summary>Uploads a new 100x100 PNG news icon.</summary>
    [BlueAuthorize(BluePermission.NewsIconCreate)]
    [HttpPost]
    public Task<NewsIconDto> Create([FromForm] string name, [FromForm] IFormFile file)
    {
        return _service.CreateAsync(name, file.OpenReadStream(), file.FileName, file.ContentType);
    }

    /// <summary>Removes a news icon. Posts that already reference it keep rendering it.</summary>
    [BlueAuthorize(BluePermission.NewsIconDelete)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
