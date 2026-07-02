using Bluewater.Domain.Models.Outings;

namespace Bluewater.Core.Dto.Outings;

public record OutingParticipantDto(
    Guid UserId,
    string Fullname,
    bool HasProfilePicture,
    OutingParticipantRole Role,
    bool Invited,
    bool CheckedIn,
    DateTime? UpdatedAt);
