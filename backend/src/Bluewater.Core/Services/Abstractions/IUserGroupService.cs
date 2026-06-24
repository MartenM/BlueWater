using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupService
{
    Task<List<UserGroupDto>> ListAsync();
    Task<UserGroupDto> GetAsync(Guid id);
    Task<UserGroupDto> CreateAsync(UpsertUserGroupRequest request);
    Task<UserGroupDto> UpdateAsync(Guid id, UpsertUserGroupRequest request);
    Task DeleteAsync(Guid id);
    Task<List<UserGroupDto>> FindByNameAsync(string name);
}
