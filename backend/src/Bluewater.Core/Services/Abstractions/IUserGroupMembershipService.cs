using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupMembershipService
{
    Task<List<UserGroupMembershipDto>> GetGroupsForUserAsync(Guid userId);
}
