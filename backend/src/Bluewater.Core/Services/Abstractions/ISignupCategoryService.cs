using Bluewater.Core.Dto.Signup;

namespace Bluewater.Core.Services.Abstractions;

public interface ISignupCategoryService
{
    Task<List<SignupCategoryDto>> ListAsync();
    Task<SignupCategoryDto> GetAsync(Guid id);
    Task<SignupCategoryDto> CreateAsync(UpsertSignupCategoryRequest request);
    Task<SignupCategoryDto> UpdateAsync(Guid id, UpsertSignupCategoryRequest request);
    Task DeleteAsync(Guid id);
}
