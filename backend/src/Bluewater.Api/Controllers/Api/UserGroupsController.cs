using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for UserGroup (the group definition: name, description, category), plus
/// read endpoints listing which groups a user belongs to.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserGroupsController : ControllerBase
{
    private readonly IUserGroupService _service;
    private readonly IUserGroupMembershipService _membershipService;
    private readonly ICurrentUserService _currentUser;

    public UserGroupsController(
        IUserGroupService service,
        IUserGroupMembershipService membershipService,
        ICurrentUserService currentUser)
    {
        _service = service;
        _membershipService = membershipService;
        _currentUser = currentUser;
    }

    /// <summary>Lists all groups.</summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet]
    public Task<List<UserGroupDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Gets a single group by id.</summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet("{id:guid}")]
    public Task<UserGroupDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new group. The referenced UserGroupCategoryId must already exist.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPost]
    public Task<UserGroupDto> Create(UpsertUserGroupRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates a group's name/description/category.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPut("{id:guid}")]
    public Task<UserGroupDto> Update(Guid id, UpsertUserGroupRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>
    /// Deletes a group. Fails if any UserGroupInstance still references it (FK is Restrict)
    /// — delete those instances first.
    /// </summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    /// <summary>
    /// Finds groups with an exact (case-insensitive) name match. Duplicates are allowed, so this
    /// can return more than one match — used by the "create group" UI to hint at reusing an
    /// existing group via a new instance instead of creating another one with the same name.
    /// </summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet("by-name")]
    public Task<List<UserGroupDto>> ByName([FromQuery] string name)
    {
        return _service.FindByNameAsync(name);
    }

    /// <summary>
    /// Lists the calling user's own groups across all seasons. Just needs an authenticated
    /// user — no ViewGroups permission required, since it only exposes your own data.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public Task<List<UserGroupMembershipDto>> Me()
    {
        return _membershipService.GetGroupsForUserAsync(_currentUser.Id);
    }

    /// <summary>Lists the groups a specific user belongs to across all seasons.</summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet("users/{userId:guid}")]
    public Task<List<UserGroupMembershipDto>> ForUser(Guid userId)
    {
        return _membershipService.GetGroupsForUserAsync(userId);
    }
}
