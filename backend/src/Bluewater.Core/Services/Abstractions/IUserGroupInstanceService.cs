using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupInstanceService
{
    Task<List<UserGroupInstanceDto>> ListAsync();
    Task<UserGroupInstanceDto> GetAsync(Guid id);
    Task<UserGroupInstanceDto> CreateAsync(CreateUserGroupInstanceRequest request);
    Task DeleteAsync(Guid id);

    Task AddMemberAsync(Guid instanceId, Guid userId);
    Task RemoveMemberAsync(Guid instanceId, Guid userId);
    Task AssignMemberRoleAsync(Guid instanceId, Guid userId, Guid? roleId);
}
