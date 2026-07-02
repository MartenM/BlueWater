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

    public async Task<List<UserGroupCategoryOverviewDto>> GetOverviewAsync(Guid? seasonId)
    {
        var categories = await _db.UserGroupCategories
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync();

        if (seasonId is null)
        {
            var allGroups = await _db.UserGroups
                .Select(g => new { g.Id, g.Name, g.UserGroupCategoryId })
                .ToListAsync();

            return categories
                .Select(c =>
                {
                    var groups = allGroups
                        .Where(g => g.UserGroupCategoryId == c.Id)
                        .Select(g => new UserGroupOverviewDto(g.Id, g.Name, null, null, null))
                        .ToList();
                    return new UserGroupCategoryOverviewDto(c.Id, c.Name, c.Description, groups.Count, groups);
                })
                .ToList();
        }

        var instances = await _db.UserGroupInstances
            .Where(x => x.SeasonId == seasonId.Value)
            .Select(x => new
            {
                x.Id,
                x.UserGroupId,
                x.UserGroup.Name,
                x.UserGroup.UserGroupCategoryId,
                MemberCount = x.Members.Count,
                PermissionCount = x.UserGroup.Permissions.Count
            })
            .ToListAsync();

        return categories
            .Select(c =>
            {
                var groups = instances
                    .Where(i => i.UserGroupCategoryId == c.Id)
                    .Select(i => new UserGroupOverviewDto(i.UserGroupId, i.Name, i.Id, i.MemberCount, i.PermissionCount))
                    .ToList();
                return new UserGroupCategoryOverviewDto(c.Id, c.Name, c.Description, groups.Count, groups);
            })
            .ToList();
    }

    private async Task<UserGroupCategory> Find(Guid id)
    {
        return await _db.UserGroupCategories.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroupCategory '{id}' was not found.");
    }

    private static UserGroupCategoryDto ToDto(UserGroupCategory x) =>
        new(x.Id, x.Name, x.Description);
}
