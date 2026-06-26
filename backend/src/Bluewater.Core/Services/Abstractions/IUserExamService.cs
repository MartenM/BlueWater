using Bluewater.Core.Dto.Exams;

namespace Bluewater.Core.Services.Abstractions;

public interface IUserExamService
{
    Task<List<UserExamDto>> ListByUserAsync(Guid userId);
    Task<List<UserExamDto>> ListByExamTypeAsync(Guid examTypeId);
    Task<UserExamDto> AssignAsync(AssignExamRequest request);
    Task UnassignAsync(Guid userExamId);
}
