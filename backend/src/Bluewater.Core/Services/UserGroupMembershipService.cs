using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserGroupMembershipService : IUserGroupMembershipService
{
    private readonly BluewaterContext _db;

    public UserGroupMembershipService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<UserGroupMembershipDto>> GetGroupsForUserAsync(Guid userId)
    {
        var instances = await _db.UserGroupInstances
            .Where(x => x.Members.Any(m => m.UserId == userId))
            .Include(x => x.UserGroup).ThenInclude(g => g.UserGroupCategory)
            .Include(x => x.Season)
            .Include(x => x.Members.Where(m => m.UserId == userId)).ThenInclude(m => m.UserGroupCategoryRole)
            .ToListAsync();

        return instances
            .Select(x => new UserGroupMembershipDto(
                x.UserGroupId,
                x.Season.Name,
                x.UserGroup.UserGroupCategory.Name,
                x.UserGroup.Name,
                x.Members.FirstOrDefault(m => m.UserId == userId)?.UserGroupCategoryRole?.NamePlural))
            .ToList();
    }
}
