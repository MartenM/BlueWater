using Bluewater.Core.Dto.Groups;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserGroupCategoryService
{
    Task<List<UserGroupCategoryDto>> ListAsync();
    Task<UserGroupCategoryDto> GetAsync(Guid id);
    Task<UserGroupCategoryDto> CreateAsync(UpsertUserGroupCategoryRequest request);
    Task<UserGroupCategoryDto> UpdateAsync(Guid id, UpsertUserGroupCategoryRequest request);
    Task DeleteAsync(Guid id);
}
