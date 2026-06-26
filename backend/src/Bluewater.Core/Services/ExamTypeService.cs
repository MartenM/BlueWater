using Bluewater.Core.Dto.Exams;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Exams;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class ExamTypeService : IExamTypeService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertExamTypeRequest> _validator;

    public ExamTypeService(BluewaterContext db, IValidator<UpsertExamTypeRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<ExamTypeDto>> ListAsync()
    {
        return await _db.ExamTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ExamTypeDto(x.Id, x.Name, x.Description))
            .ToListAsync();
    }

    public async Task<ExamTypeDto> GetAsync(Guid id)
    {
        var examType = await _db.ExamTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"ExamType '{id}' was not found.");

        return ToDto(examType);
    }

    public async Task<ExamTypeDto> CreateAsync(UpsertExamTypeRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var examType = new ExamType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        _db.ExamTypes.Add(examType);
        await _db.SaveChangesAsync();

        return await GetAsync(examType.Id);
    }

    public async Task<ExamTypeDto> UpdateAsync(Guid id, UpsertExamTypeRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var examType = await Find(id);

        examType.Name = request.Name;
        examType.Description = request.Description;

        await _db.SaveChangesAsync();

        return await GetAsync(examType.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var examType = await Find(id);

        _db.ExamTypes.Remove(examType);
        await _db.SaveChangesAsync();
    }

    private async Task<ExamType> Find(Guid id)
    {
        return await _db.ExamTypes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"ExamType '{id}' was not found.");
    }

    private static ExamTypeDto ToDto(ExamType x) => new(x.Id, x.Name, x.Description);
}
