using Bluewater.Core.Dto.Profile;
using Bluewater.Domain.Models.Files;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(Guid userId);
    Task<List<ActiveMemberDto>> SearchActiveAsync(string? search, CancellationToken ct = default);
    Task SetProfilePictureAsync(Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<(StoredFile Metadata, Stream Content)> GetProfilePictureAsync(Guid userId, CancellationToken cancellationToken = default);
}
