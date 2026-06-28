using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Signup;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class SignupCategoryService : ISignupCategoryService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertSignupCategoryRequest> _validator;

    public SignupCategoryService(BluewaterContext db, IValidator<UpsertSignupCategoryRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<SignupCategoryDto>> ListAsync()
    {
        var categories = await _db.SignupCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .ToListAsync();

        return categories.Select(ToDto).ToList();
    }

    public async Task<SignupCategoryDto> GetAsync(Guid id)
    {
        var category = await Find(id);
        return ToDto(category);
    }

    public async Task<SignupCategoryDto> CreateAsync(UpsertSignupCategoryRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var category = new SignupCategory
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Hidden = request.Hidden,
            SortOrder = request.SortOrder,
        };

        _db.SignupCategories.Add(category);
        await _db.SaveChangesAsync();

        return ToDto(category);
    }

    public async Task<SignupCategoryDto> UpdateAsync(Guid id, UpsertSignupCategoryRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var category = await Find(id);

        category.Title = request.Title;
        category.Hidden = request.Hidden;
        category.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return ToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await Find(id);
        _db.SignupCategories.Remove(category);
        await _db.SaveChangesAsync();
    }

    private async Task<SignupCategory> Find(Guid id) =>
        await _db.SignupCategories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new BlueNotFoundException($"SignupCategory '{id}' was not found.");

    private static SignupCategoryDto ToDto(SignupCategory c) =>
        new(c.Id, c.Title, c.Hidden, c.SortOrder);
}
