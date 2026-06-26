using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto.Exams;

public record UpsertExamTypeRequest(
    [Required] string Name,
    string Description);
