using Bluewater.Core.Dto.Exams;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class AssignExamRequestValidator : AbstractValidator<AssignExamRequest>
{
    public AssignExamRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.UserId)
            .MustAsync(async (userId, ct) => await db.Users.AnyAsync(u => u.Id == userId, ct))
            .WithMessage(x => $"User '{x.UserId}' does not exist.");

        RuleFor(x => x.ExamTypeId)
            .MustAsync(async (examTypeId, ct) => await db.ExamTypes.AnyAsync(e => e.Id == examTypeId, ct))
            .WithMessage(x => $"ExamType '{x.ExamTypeId}' does not exist.");

        RuleFor(x => x)
            .MustAsync(async (req, ct) =>
                !await db.UserExams.AnyAsync(e => e.UserId == req.UserId && e.ExamTypeId == req.ExamTypeId, ct))
            .WithName("ExamTypeId")
            .WithMessage("This user has already obtained this exam.");
    }
}
