using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.News;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for news posts. Listing/reading is open to anonymous callers - members-only posts
/// are hidden unless the caller has the NewsView permission, decided inside INewsService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsService _service;

    public NewsController(INewsService service)
    {
        _service = service;
    }

    /// <summary>Lists news posts, newest first.</summary>
    [HttpGet]
    public Task<PagedResult<NewsPostDto>> List(int page = 1, int pageSize = 20)
    {
        return _service.ListAsync(page, pageSize);
    }

    /// <summary>Gets a single news post by id.</summary>
    [HttpGet("{id:guid}")]
    public Task<NewsPostDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new news post.</summary>
    [BlueAuthorize(BluePermission.NewsModify)]
    [HttpPost]
    public Task<NewsPostDto> Create(UpsertNewsPostRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates a news post.</summary>
    [BlueAuthorize(BluePermission.NewsModify)]
    [HttpPut("{id:guid}")]
    public Task<NewsPostDto> Update(Guid id, UpsertNewsPostRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>Deletes a news post.</summary>
    [BlueAuthorize(BluePermission.NewsModify)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
