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
        var groups = await _db.UserGroups
            .AsNoTracking()
            .Include(x => x.Permissions)
            .Include(x => x.UserGroupCategory)
            .ToListAsync();

        return groups.Select(ToDto).ToList();
    }

    public async Task<UserGroupDto> GetAsync(Guid id)
    {
        var group = await _db.UserGroups
            .AsNoTracking()
            .Include(x => x.Permissions)
            .Include(x => x.UserGroupCategory)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroup '{id}' was not found.");

        return ToDto(group);
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

    public async Task<List<UserGroupDto>> FindByNameAsync(string name)
    {
        var normalized = name.Trim().ToLower();
        var groups = await _db.UserGroups
            .AsNoTracking()
            .Include(x => x.Permissions)
            .Include(x => x.UserGroupCategory)
            .Where(x => x.Name.ToLower() == normalized)
            .ToListAsync();

        return groups.Select(ToDto).ToList();
    }

    public async Task AssignPermissionAsync(Guid groupId, BluePermission permission, Guid? roleId)
    {
        await Find(groupId);

        if (roleId.HasValue)
        {
            var roleExists = await _db.UserGroupCategoryRoles.AnyAsync(x => x.Id == roleId.Value);
            if (!roleExists)
                throw new BlueValidationException($"UserGroupCategoryRole '{roleId}' does not exist.");
        }

        var existing = await _db.UserGroupPermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserGroupId == groupId
                                   && x.Permission == permission
                                   && x.UserGroupCategoryRoleId == roleId);

        if (existing != null)
        {
            if (existing.DeletedAt == null)
                return; // already active

            existing.DeletedAt = null;
            existing.DeletedByUserId = null;
            await _db.SaveChangesAsync();
            return;
        }

        _db.UserGroupPermissions.Add(new UserGroupPermission
        {
            Id = Guid.NewGuid(),
            UserGroupId = groupId,
            Permission = permission,
            UserGroupCategoryRoleId = roleId
        });

        await _db.SaveChangesAsync();
    }

    public async Task RevokePermissionAsync(Guid groupId, BluePermission permission, Guid? roleId)
    {
        var assignment = await _db.UserGroupPermissions
            .FirstOrDefaultAsync(x => x.UserGroupId == groupId
                                   && x.Permission == permission
                                   && x.UserGroupCategoryRoleId == roleId);

        if (assignment == null)
            return;

        _db.UserGroupPermissions.Remove(assignment);
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
            throw new BlueValidationException($"UserGroupCategory '{categoryId}' does not exist.");
    }

    private static UserGroupDto ToDto(UserGroup x) =>
        new(x.Id,
            x.Name,
            x.Description,
            x.UserGroupCategoryId,
            x.UserGroupCategory?.Name ?? string.Empty,
            x.Permissions.Select(p => new UserGroupPermissionDto(p.Permission, p.UserGroupCategoryRoleId)).ToList());
}
