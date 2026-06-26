namespace Bluewater.Core.Dto.Exams;

public record UserExamDto(
    Guid Id,
    Guid UserId,
    string UserFullname,
    Guid ExamTypeId,
    string ExamTypeName,
    DateOnly ObtainedAt);
