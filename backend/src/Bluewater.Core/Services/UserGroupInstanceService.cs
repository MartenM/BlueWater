using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserGroupInstanceService : IUserGroupInstanceService
{
    private readonly BluewaterContext _db;

    public UserGroupInstanceService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<UserGroupInstanceDto>> ListAsync()
    {
        var instances = await WithDetails().ToListAsync();
        return instances.Select(ToDto).ToList();
    }

    public async Task<UserGroupInstanceDto> GetAsync(Guid id)
    {
        var instance = await WithDetails().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroupInstance '{id}' was not found.");

        return ToDto(instance);
    }

    public async Task<UserGroupInstanceDto> CreateAsync(CreateUserGroupInstanceRequest request)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(x => x.Id == request.UserGroupId);
        if (group is null)
            throw new BlueValidationException($"UserGroup '{request.UserGroupId}' does not exist.");

        var seasonExists = await _db.Seasons.AnyAsync(x => x.Id == request.SeasonId);
        if (!seasonExists)
            throw new BlueValidationException($"Season '{request.SeasonId}' does not exist.");

        var alreadyExists = await _db.UserGroupInstances
            .AnyAsync(x => x.UserGroupId == request.UserGroupId && x.SeasonId == request.SeasonId);
        if (alreadyExists)
            throw new BlueValidationException("A UserGroupInstance for this group and season already exists.");

        var nameConflict = await _db.UserGroupInstances
            .Where(x => x.SeasonId == request.SeasonId)
            .AnyAsync(x => x.UserGroup.Name.ToLower() == group.Name.ToLower());
        if (nameConflict)
            throw new BlueValidationException(
                $"A UserGroupInstance for a group named '{group.Name}' already exists in this season.");

        var instance = new UserGroupInstance
        {
            Id = Guid.NewGuid(),
            UserGroupId = request.UserGroupId,
            SeasonId = request.SeasonId
        };

        _db.UserGroupInstances.Add(instance);
        await _db.SaveChangesAsync();

        return await GetAsync(instance.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var instance = await Find(id);

        _db.UserGroupInstances.Remove(instance);
        await _db.SaveChangesAsync();
    }

    public async Task AddMemberAsync(Guid instanceId, Guid userId)
    {
        await Find(instanceId);

        var userExists = await _db.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
            throw new BlueValidationException($"User '{userId}' does not exist.");

        var alreadyMember = await _db.UserGroupInstanceMembers
            .AnyAsync(x => x.UserGroupInstanceId == instanceId && x.UserId == userId);
        if (alreadyMember)
            return;

        _db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember
        {
            UserGroupInstanceId = instanceId,
            UserId = userId
        });

        await _db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid instanceId, Guid userId)
    {
        var member = await _db.UserGroupInstanceMembers
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.UserId == userId);

        if (member == null)
            return;

        _db.UserGroupInstanceMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task AssignMemberRoleAsync(Guid instanceId, Guid userId, Guid? roleId)
    {
        var member = await _db.UserGroupInstanceMembers
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.UserId == userId)
            ?? throw new BlueNotFoundException($"User '{userId}' is not a member of instance '{instanceId}'.");

        if (roleId.HasValue)
        {
            var instance = await _db.UserGroupInstances
                .Include(x => x.UserGroup)
                .FirstAsync(x => x.Id == instanceId);

            var roleExists = await _db.UserGroupCategoryRoles
                .AnyAsync(x => x.Id == roleId.Value
                             && x.UserGroupCategoryId == instance.UserGroup.UserGroupCategoryId);
            if (!roleExists)
                throw new BlueValidationException(
                    $"UserGroupCategoryRole '{roleId}' does not belong to this group's category.");
        }

        member.UserGroupCategoryRoleId = roleId;
        await _db.SaveChangesAsync();
    }

    private async Task<UserGroupInstance> Find(Guid id)
    {
        return await _db.UserGroupInstances.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"UserGroupInstance '{id}' was not found.");
    }

    private IQueryable<UserGroupInstance> WithDetails() =>
        _db.UserGroupInstances
            .AsNoTracking()
            .Include(x => x.UserGroup)
                .ThenInclude(g => g.Permissions)
            .Include(x => x.Season)
            .Include(x => x.Members);

    private static UserGroupInstanceDto ToDto(UserGroupInstance x) =>
        new(x.Id,
            x.UserGroupId,
            x.UserGroup.Name,
            x.UserGroup.UserGroupCategoryId,
            x.SeasonId,
            x.Season.Name,
            x.UserGroup.Permissions.Select(p => new UserGroupPermissionDto(p.Permission, p.UserGroupCategoryRoleId)).ToList(),
            x.Members.Select(m => new InstanceMemberDto(m.UserId, m.UserGroupCategoryRoleId)).ToList());
}
