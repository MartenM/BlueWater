using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserGroupService : IUserGroupService
{
    private readonly BluewaterContext _db;

    public UserGroupService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<UserGroupDto>> ListAsync()
    {
        return await _db.UserGroups
            .Select(ProjectToDto)
            .ToListAsync();
    }

    public async Task<UserGroupDto> GetAsync(Guid id)
    {
        return await _db.UserGroups
            .Where(x => x.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync()
            ?? throw new BlueNotFoundException($"UserGroup '{id}' was not found.");
    }

    public async Task<UserGroupDto> CreateAsync(UpsertUserGroupRequest request)
    {
        await EnsureCategoryExists(request.UserGroupCategoryId);

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            UserGroupCategoryId = request.UserGroupCategoryId
        };

        _db.UserGroups.Add(group);
        await _db.SaveChangesAsync();

        return await GetAsync(group.Id);
    }

    public async Task<UserGroupDto> UpdateAsync(Guid id, UpsertUserGroupRequest request)
    {
        await EnsureCategoryExists(request.UserGroupCategoryId);

        var group = await Find(id);

        group.Name = request.Name;
        group.Description = request.Description;
        group.UserGroupCategoryId = request.UserGroupCategoryId;

        await _db.SaveChangesAsync();

        return await GetAsync(group.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await Find(id);

        _db.UserGroups.Remove(group);
        await _db.SaveChangesAsync();
    }

    private async Task<UserGroup> Find(Guid id)
    {
        return await _db.UserGroups.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroup '{id}' was not found.");
    }

    private async Task EnsureCategoryExists(Guid categoryId)
    {
        var exists = await _db.UserGroupCategories.AnyAsync(x => x.Id == categoryId);
        if (!exists)
        {
            throw new BlueValidationException($"UserGroupCategory '{categoryId}' does not exist.");
        }
    }

    private static readonly System.Linq.Expressions.Expression<Func<UserGroup, UserGroupDto>> ProjectToDto =
        x => new UserGroupDto(x.Id, x.Name, x.Description, x.UserGroupCategoryId, x.UserGroupCategory.Name);
}
