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
        var groupExists = await _db.UserGroups.AnyAsync(x => x.Id == request.UserGroupId);
        if (!groupExists)
        {
            throw new BlueValidationException($"UserGroup '{request.UserGroupId}' does not exist.");
        }

        var seasonExists = await _db.Seasons.AnyAsync(x => x.Id == request.SeasonId);
        if (!seasonExists)
        {
            throw new BlueValidationException($"Season '{request.SeasonId}' does not exist.");
        }

        var alreadyExists = await _db.UserGroupInstances
            .AnyAsync(x => x.UserGroupId == request.UserGroupId && x.SeasonId == request.SeasonId);
        if (alreadyExists)
        {
            throw new BlueValidationException("A UserGroupInstance for this group and season already exists.");
        }

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
        {
            throw new BlueValidationException($"User '{userId}' does not exist.");
        }

        var existing = await _db.UserGroupInstanceMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.UserId == userId);

        if (existing == null)
        {
            _db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember
            {
                UserGroupInstanceId = instanceId,
                UserId = userId
            });
        }
        else if (existing.DeletedAt != null)
        {
            // Row already exists (soft-deleted) under this composite key; revive it
            // instead of inserting a duplicate, which would violate the primary key.
            existing.DeletedAt = null;
            existing.DeletedByUserId = null;
        }
        else
        {
            return;
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid instanceId, Guid userId)
    {
        var member = await _db.UserGroupInstanceMembers
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.UserId == userId);

        if (member == null)
        {
            return;
        }

        _db.UserGroupInstanceMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task AssignPermissionAsync(Guid instanceId, BluePermission permission)
    {
        await Find(instanceId);

        var existing = await _db.UserGroupInstancePermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.Permission == permission);

        if (existing == null)
        {
            _db.UserGroupInstancePermissions.Add(new UserGroupInstancePermission
            {
                UserGroupInstanceId = instanceId,
                Permission = permission
            });
        }
        else if (existing.DeletedAt != null)
        {
            // Row already exists (soft-deleted) under this composite key; revive it
            // instead of inserting a duplicate, which would violate the primary key.
            existing.DeletedAt = null;
            existing.DeletedByUserId = null;
        }
        else
        {
            return;
        }

        await _db.SaveChangesAsync();
    }

    public async Task RevokePermissionAsync(Guid instanceId, BluePermission permission)
    {
        var assignment = await _db.UserGroupInstancePermissions
            .FirstOrDefaultAsync(x => x.UserGroupInstanceId == instanceId && x.Permission == permission);

        if (assignment == null)
        {
            return;
        }

        _db.UserGroupInstancePermissions.Remove(assignment);
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
            .Include(x => x.Season)
            .Include(x => x.Permissions)
            .Include(x => x.Members);

    private static UserGroupInstanceDto ToDto(UserGroupInstance x) =>
        new(
            x.Id,
            x.UserGroupId,
            x.UserGroup.Name,
            x.SeasonId,
            x.Season.Name,
            x.Permissions.Select(p => p.Permission).ToList(),
            x.Members.Select(m => m.UserId).ToList());
}
