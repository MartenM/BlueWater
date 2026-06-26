using Bluewater.Core.Dto.Exams;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Exams;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserExamService : IUserExamService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<AssignExamRequest> _validator;

    public UserExamService(BluewaterContext db, IValidator<AssignExamRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<UserExamDto>> ListByUserAsync(Guid userId)
    {
        var items = await _db.UserExams
            .AsNoTracking()
            .Include(x => x.ExamType)
            .Include(x => x.User)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ObtainedAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<List<UserExamDto>> ListByExamTypeAsync(Guid examTypeId)
    {
        var items = await _db.UserExams
            .AsNoTracking()
            .Include(x => x.ExamType)
            .Include(x => x.User)
            .Where(x => x.ExamTypeId == examTypeId)
            .OrderBy(x => x.ObtainedAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<UserExamDto> AssignAsync(AssignExamRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var userExam = new UserExam
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ExamTypeId = request.ExamTypeId,
            ObtainedAt = request.ObtainedAt
        };

        _db.UserExams.Add(userExam);
        await _db.SaveChangesAsync();

        return await GetAsync(userExam.Id);
    }

    public async Task UnassignAsync(Guid userExamId)
    {
        var userExam = await _db.UserExams.FirstOrDefaultAsync(x => x.Id == userExamId)
            ?? throw new BlueNotFoundException($"UserExam '{userExamId}' was not found.");

        _db.UserExams.Remove(userExam);
        await _db.SaveChangesAsync();
    }

    private async Task<UserExamDto> GetAsync(Guid id)
    {
        var userExam = await _db.UserExams
            .AsNoTracking()
            .Include(x => x.ExamType)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserExam '{id}' was not found.");

        return ToDto(userExam);
    }

    private static UserExamDto ToDto(UserExam x) =>
        new(x.Id, x.UserId, x.User.Fullname, x.ExamTypeId, x.ExamType.Name, x.ObtainedAt);
}
