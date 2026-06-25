using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserGroupCategoryRoleService : IUserGroupCategoryRoleService
{
    private readonly BluewaterContext _db;

    public UserGroupCategoryRoleService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<UserGroupCategoryRoleDto>> ListAsync(Guid categoryId)
    {
        await EnsureCategoryExists(categoryId);

        return await _db.UserGroupCategoryRoles
            .Where(x => x.UserGroupCategoryId == categoryId)
            .OrderBy(x => x.SortOrder)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<UserGroupCategoryRoleDto> GetAsync(Guid roleId)
    {
        return await _db.UserGroupCategoryRoles
            .Where(x => x.Id == roleId)
            .Select(ToDto)
            .FirstOrDefaultAsync()
            ?? throw new BlueNotFoundException($"UserGroupCategoryRole '{roleId}' was not found.");
    }

    public async Task<UserGroupCategoryRoleDto> CreateAsync(Guid categoryId, UpsertUserGroupCategoryRoleRequest request)
    {
        await EnsureCategoryExists(categoryId);

        var sortOrder = await _db.UserGroupCategoryRoles.CountAsync(x => x.UserGroupCategoryId == categoryId);

        var role = new UserGroupCategoryRole
        {
            Id = Guid.NewGuid(),
            UserGroupCategoryId = categoryId,
            SortOrder = sortOrder,
            NamePlural = request.NamePlural,
            NameMasculine = request.NameMasculine,
            NameFeminine = request.NameFeminine
        };

        _db.UserGroupCategoryRoles.Add(role);
        await _db.SaveChangesAsync();

        return await GetAsync(role.Id);
    }

    public async Task<UserGroupCategoryRoleDto> UpdateAsync(Guid roleId, UpsertUserGroupCategoryRoleRequest request)
    {
        var role = await Find(roleId);

        role.NamePlural = request.NamePlural;
        role.NameMasculine = request.NameMasculine;
        role.NameFeminine = request.NameFeminine;

        await _db.SaveChangesAsync();

        return await GetAsync(role.Id);
    }

    public async Task DeleteAsync(Guid roleId)
    {
        var role = await Find(roleId);

        _db.UserGroupCategoryRoles.Remove(role);
        await _db.SaveChangesAsync();
    }

    public async Task ReorderAsync(Guid categoryId, ReorderRolesRequest request)
    {
        await EnsureCategoryExists(categoryId);

        var roles = await _db.UserGroupCategoryRoles
            .Where(r => r.UserGroupCategoryId == categoryId)
            .ToListAsync();

        for (var i = 0; i < request.RoleIds.Count; i++)
        {
            var role = roles.FirstOrDefault(r => r.Id == request.RoleIds[i]);
            if (role != null)
                role.SortOrder = i;
        }

        await _db.SaveChangesAsync();
    }

    private async Task<UserGroupCategoryRole> Find(Guid roleId)
    {
        return await _db.UserGroupCategoryRoles.FirstOrDefaultAsync(x => x.Id == roleId)
            ?? throw new BlueNotFoundException($"UserGroupCategoryRole '{roleId}' was not found.");
    }

    private async Task EnsureCategoryExists(Guid categoryId)
    {
        var exists = await _db.UserGroupCategories.AnyAsync(x => x.Id == categoryId);
        if (!exists)
            throw new BlueNotFoundException($"UserGroupCategory '{categoryId}' was not found.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<UserGroupCategoryRole, UserGroupCategoryRoleDto>> ToDto =
        x => new UserGroupCategoryRoleDto(x.Id, x.UserGroupCategoryId, x.NamePlural, x.NameMasculine, x.NameFeminine);
}
