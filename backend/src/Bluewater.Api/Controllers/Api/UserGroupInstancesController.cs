using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for UserGroupInstance — a UserGroup scoped to one season, with its own
/// member list and permission assignments. Permissions assigned here are only
/// granted to a member's JWT while their instance's season is the app's current season.
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

    /// <summary>Gets a single instance, including its members and assigned permissions.</summary>
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

    /// <summary>Deletes an instance. Cascades to its members and permission assignments.</summary>
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
    /// Assigns a permission to the instance. Takes effect for members' JWTs on their
    /// next login/refresh once this is the current season. No-op if already assigned.
    /// </summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpPost("{id:guid}/permissions")]
    public Task AssignPermission(Guid id, AssignPermissionRequest request)
    {
        return _service.AssignPermissionAsync(id, request.Permission);
    }

    /// <summary>Revokes a permission from the instance. No-op if not assigned.</summary>
    [BlueAuthorize(BluePermission.AdminModifyGroups)]
    [HttpDelete("{id:guid}/permissions/{permission}")]
    public Task RevokePermission(Guid id, BluePermission permission)
    {
        return _service.RevokePermissionAsync(id, permission);
    }
}
