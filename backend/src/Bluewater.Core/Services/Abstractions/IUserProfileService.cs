using Bluewater.Core.Dto.Profile;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(Guid userId);
    Task SetProfilePictureAsync(Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
