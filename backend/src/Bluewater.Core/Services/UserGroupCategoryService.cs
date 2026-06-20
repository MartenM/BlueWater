using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserGroupCategoryService : IUserGroupCategoryService
{
    private readonly BluewaterContext _db;

    public UserGroupCategoryService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<UserGroupCategoryDto>> ListAsync()
    {
        return await _db.UserGroupCategories
            .Select(x => new UserGroupCategoryDto(x.Id, x.Name, x.Description))
            .ToListAsync();
    }

    public async Task<UserGroupCategoryDto> GetAsync(Guid id)
    {
        var category = await Find(id);
        return ToDto(category);
    }

    public async Task<UserGroupCategoryDto> CreateAsync(UpsertUserGroupCategoryRequest request)
    {
        var category = new UserGroupCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        _db.UserGroupCategories.Add(category);
        await _db.SaveChangesAsync();

        return ToDto(category);
    }

    public async Task<UserGroupCategoryDto> UpdateAsync(Guid id, UpsertUserGroupCategoryRequest request)
    {
        var category = await Find(id);

        category.Name = request.Name;
        category.Description = request.Description;

        await _db.SaveChangesAsync();

        return ToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await Find(id);

        _db.UserGroupCategories.Remove(category);
        await _db.SaveChangesAsync();
    }

    private async Task<UserGroupCategory> Find(Guid id)
    {
        return await _db.UserGroupCategories.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroupCategory '{id}' was not found.");
    }

    private static UserGroupCategoryDto ToDto(UserGroupCategory x) =>
        new(x.Id, x.Name, x.Description);
}
