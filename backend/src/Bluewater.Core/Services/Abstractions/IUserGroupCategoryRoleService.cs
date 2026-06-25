using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupCategoryRoleService
{
    Task<List<UserGroupCategoryRoleDto>> ListAsync(Guid categoryId);
    Task<UserGroupCategoryRoleDto> GetAsync(Guid roleId);
    Task<UserGroupCategoryRoleDto> CreateAsync(Guid categoryId, UpsertUserGroupCategoryRoleRequest request);
    Task<UserGroupCategoryRoleDto> UpdateAsync(Guid roleId, UpsertUserGroupCategoryRoleRequest request);
    Task DeleteAsync(Guid roleId);
    Task ReorderAsync(Guid categoryId, ReorderRolesRequest request);
}
