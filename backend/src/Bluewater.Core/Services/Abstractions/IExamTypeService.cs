using Bluewater.Core.Dto.Exams;

namespace Bluewater.Core.Services.Abstractions;

public interface IExamTypeService
{
    Task<List<ExamTypeDto>> ListAsync();
    Task<ExamTypeDto> GetAsync(Guid id);
    Task<ExamTypeDto> CreateAsync(UpsertExamTypeRequest request);
    Task<ExamTypeDto> UpdateAsync(Guid id, UpsertExamTypeRequest request);
    Task DeleteAsync(Guid id);
}
