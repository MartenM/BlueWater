using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Dto.Profile;

public record UserProfileDto(
    Guid Id,
    string Firstname,
    string SurnamePrefix,
    string Surname,
    string Fullname,
    List<UserGroupMembershipDto> Groups);
