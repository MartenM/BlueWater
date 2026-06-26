using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto.Exams;

public record AssignExamRequest(
    [Required] Guid UserId,
    [Required] Guid ExamTypeId,
    [Required] DateOnly ObtainedAt);
