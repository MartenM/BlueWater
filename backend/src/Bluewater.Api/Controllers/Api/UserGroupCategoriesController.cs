using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for UserGroupCategory, the functional grouping used to classify UserGroups
/// (e.g. Main groups, Committees, Rowing groups, Houses).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserGroupCategoriesController : ControllerBase
{
    private readonly IUserGroupCategoryService _service;

    public UserGroupCategoriesController(IUserGroupCategoryService service)
    {
        _service = service;
    }

    /// <summary>Lists all categories.</summary>
    [BlueAuthorize(BluePermission.ViewGroups)]
    [HttpGet]
    public Task<List<UserGroupCategoryDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Gets a single category by id.</summary>
    [BlueAuthorize(BluePermission.ViewGroups)]
    [HttpGet("{id:guid}")]
    public Task<UserGroupCategoryDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new category.</summary>
    [BlueAuthorize(BluePermission.ModifyGroups)]
    [HttpPost]
    public Task<UserGroupCategoryDto> Create(UpsertUserGroupCategoryRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates a category's name/description.</summary>
    [BlueAuthorize(BluePermission.ModifyGroups)]
    [HttpPut("{id:guid}")]
    public Task<UserGroupCategoryDto> Update(Guid id, UpsertUserGroupCategoryRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>
    /// Deletes a category. Fails if any UserGroup still references it (FK is Restrict, not Cascade)
    /// — re-categorize or delete those groups first.
    /// </summary>
    [BlueAuthorize(BluePermission.ModifyGroups)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
