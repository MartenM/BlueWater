using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Users;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for BlueUser accounts. Group/role membership is managed separately via
/// UserGroupInstancesController/IUserGroupMembershipService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    private readonly IUserProfileService _profileService;

    public UsersController(IUserService service, IUserProfileService profileService)
    {
        _service = service;
        _profileService = profileService;
    }

    /// <summary>Lists users, optionally filtered by a free-text search over name/username/email.</summary>
    [BlueAuthorize(BluePermission.AdminUsersView)]
    [HttpGet]
    [EndpointName("ListUsers")]
    public Task<PagedResult<UserDto>> List(int page = 1, int pageSize = 20, string? search = null)
    {
        return _service.ListAsync(page, pageSize, search);
    }

    /// <summary>Gets a single user by id.</summary>
    [BlueAuthorize(BluePermission.AdminUsersView)]
    [HttpGet("{id:guid}")]
    [EndpointName("GetUser")]
    public Task<UserDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new user with a server-generated password, returned once for the admin to share.</summary>
    [BlueAuthorize(BluePermission.AdminUsersModify)]
    [HttpPost]
    [EndpointName("CreateUser")]
    public Task<CreateUserResponse> Create(CreateUserRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates a user's information.</summary>
    [BlueAuthorize(BluePermission.AdminUsersModify)]
    [HttpPut("{id:guid}")]
    [EndpointName("UpdateUser")]
    public Task<UserDto> Update(Guid id, UpdateUserRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>Deletes a user.</summary>
    [BlueAuthorize(BluePermission.AdminUsersModify)]
    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteUser")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    /// <summary>Uploads a 75x100 profile picture for a user.</summary>
    [BlueAuthorize(BluePermission.AdminUsersModify)]
    [HttpPost("{id:guid}/picture")]
    [EndpointName("SetUserPicture")]
    public Task SetPicture(Guid id, [FromForm] IFormFile file)
    {
        return _profileService.SetProfilePictureAsync(id, file.OpenReadStream(), file.FileName, file.ContentType);
    }
}
