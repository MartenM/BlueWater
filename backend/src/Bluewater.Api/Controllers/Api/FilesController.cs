using Bluewater.Domain.Models.Files;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// Generic upload/download primitive for binary files (profile icons, news images, etc.),
/// backed by <see cref="IFileStorageService"/>. Feature-specific validation (allowed content
/// types, max size) belongs to the feature that references the returned <see cref="StoredFile.Id"/>,
/// not here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _service;

    public FilesController(IFileStorageService service)
    {
        _service = service;
    }

    /// <summary>Uploads a file and returns its stored metadata, including the id to reference it by.</summary>
    [Authorize]
    [HttpPost]
    public Task<StoredFile> Upload([FromForm] IFormFile file)
    {
        return _service.StoreAsync(file.OpenReadStream(), file.FileName, file.ContentType);
    }

    /// <summary>Streams a previously uploaded file's content back, e.g. for use as an &lt;img src&gt;.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (metadata, content) = await _service.RetrieveAsync(id);
        return File(content, metadata.ContentType, metadata.OriginalFileName);
    }

    /// <summary>Deletes a previously uploaded file.</summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
