using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for UserGroupInstance — a UserGroup scoped to one season, with its own
/// member list. Permissions are defined on the UserGroup itself (not the instance)
/// and are effective for members' JWTs while the instance's season is the current season.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserGroupInstancesController : ControllerBase
{
    private readonly IUserGroupInstanceService _service;

    public UserGroupInstancesController(IUserGroupInstanceService service)
    {
        _service = service;
    }

    /// <summary>Lists all group instances, across all seasons.</summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet]
    public Task<List<UserGroupInstanceDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Gets a single instance, including its members and the group's permissions.</summary>
    [BlueAuthorize(BluePermission.AdminViewGroups)]
    [HttpGet("{id:guid}")]
    public Task<UserGroupInstanceDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>
    /// Creates an instance for a (UserGroupId, SeasonId) pair. Both must already exist,
    /// and only one instance per group/season combination is allowed.
    /// </summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPost]
    public Task<UserGroupInstanceDto> Create(CreateUserGroupInstanceRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Deletes an instance. Cascades to its members.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    /// <summary>Adds a user as a member of the instance. No-op if already a member.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPost("{id:guid}/users/{userId:guid}")]
    public Task AddMember(Guid id, Guid userId)
    {
        return _service.AddMemberAsync(id, userId);
    }

    /// <summary>Removes a user from the instance. No-op if not a member.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpDelete("{id:guid}/users/{userId:guid}")]
    public Task RemoveMember(Guid id, Guid userId)
    {
        return _service.RemoveMemberAsync(id, userId);
    }

    /// <summary>
    /// Assigns (or clears) a role for a member within this instance.
    /// The role must belong to the same category as this instance's group.
    /// Send null RoleId to clear the member's role.
    /// </summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPut("{id:guid}/users/{userId:guid}/role")]
    public Task AssignMemberRole(Guid id, Guid userId, AssignMemberRoleRequest request)
    {
        return _service.AssignMemberRoleAsync(id, userId, request.UserGroupCategoryRoleId);
    }
}
