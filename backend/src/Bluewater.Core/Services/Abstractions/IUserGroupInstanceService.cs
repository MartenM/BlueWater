using Bluewater.Core.Dto.Groups;
using Bluewater.Domain.Models.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupInstanceService
{
    Task<List<UserGroupInstanceDto>> ListAsync();
    Task<UserGroupInstanceDto> GetAsync(Guid id);
    Task<UserGroupInstanceDto> CreateAsync(CreateUserGroupInstanceRequest request);
    Task DeleteAsync(Guid id);

    Task AddMemberAsync(Guid instanceId, Guid userId);
    Task RemoveMemberAsync(Guid instanceId, Guid userId);

    Task AssignPermissionAsync(Guid instanceId, BluePermission permission);
    Task RevokePermissionAsync(Guid instanceId, BluePermission permission);
}
