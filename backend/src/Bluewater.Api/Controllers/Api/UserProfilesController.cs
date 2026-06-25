using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Profile;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileService _service;
    private readonly ICurrentUserService _currentUser;

    public UserProfilesController(IUserProfileService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Gets the current user's own profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public Task<UserProfileDto> GetMine()
    {
        return _service.GetAsync(_currentUser.Id);
    }

    /// <summary>Gets a user's profile by id.</summary>
    [BlueAuthorize(BluePermission.ViewProfiles)]
    [HttpGet("{id:guid}")]
    public Task<UserProfileDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Streams the current user's own profile picture, e.g. for use as an &lt;img src&gt;.</summary>
    [Authorize]
    [HttpGet("me/picture")]
    [Produces("application/octet-stream")]
    [EndpointName("GetMyProfilePicture")]
    public async Task<FileResult> GetMinePicture()
    {
        var (metadata, content) = await _service.GetProfilePictureAsync(_currentUser.Id);
        return File(content, metadata.ContentType, metadata.OriginalFileName);
    }

    /// <summary>Streams a user's profile picture by id, e.g. for use as an &lt;img src&gt;.</summary>
    [BlueAuthorize(BluePermission.ViewProfiles)]
    [HttpGet("{id:guid}/picture")]
    [Produces("application/octet-stream")]
    [EndpointName("GetProfilePicture")]
    public async Task<FileResult> GetPicture(Guid id)
    {
        var (metadata, content) = await _service.GetProfilePictureAsync(id);
        return File(content, metadata.ContentType, metadata.OriginalFileName);
    }

    /// <summary>Uploads a 75x100 profile picture for a user.</summary>
    [BlueAuthorize(BluePermission.AdminModifyUsers)]
    [HttpPost("{id:guid}/picture")]
    [EndpointName("SetProfilePicture")]
    public Task SetPicture(Guid id, [FromForm] IFormFile file)
    {
        return _service.SetProfilePictureAsync(id, file.OpenReadStream(), file.FileName, file.ContentType);
    }
}
