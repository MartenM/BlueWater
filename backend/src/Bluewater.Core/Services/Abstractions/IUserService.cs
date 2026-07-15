using Bluewater.Core.Dto;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Users;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, string? search);
    Task<UserDto> GetAsync(Guid id);
    Task<CreateUserResponse> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<ApiStatusResponse> ResetUserPasswordAsync(Guid id);
    Task DeleteAsync(Guid id);
}
