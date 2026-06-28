using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MemberClusterService : IMemberClusterService
{
    private readonly BluewaterContext _db;

    public MemberClusterService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<MemberClusterDto>> ListAsync()
    {
        var clusters = await _db.MemberClusters
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroupCategory)
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroupCategoryRole)
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroup)
            .ToListAsync();

        return clusters.Select(ToDto).ToList();
    }

    public async Task<MemberClusterDto> GetAsync(Guid id)
    {
        var cluster = await FindWithCriteria(id);
        return ToDto(cluster);
    }

    public async Task<MemberClusterDto> CreateAsync(UpsertMemberClusterRequest request)
    {
        var cluster = new MemberCluster
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
        };

        _db.MemberClusters.Add(cluster);
        await _db.SaveChangesAsync();

        return ToDto(cluster);
    }

    public async Task<MemberClusterDto> UpdateAsync(Guid id, UpsertMemberClusterRequest request)
    {
        var cluster = await FindWithCriteria(id);

        cluster.Name = request.Name;
        cluster.Description = request.Description;

        await _db.SaveChangesAsync();

        return ToDto(cluster);
    }

    public async Task DeleteAsync(Guid id)
    {
        var cluster = await Find(id);
        _db.MemberClusters.Remove(cluster);
        await _db.SaveChangesAsync();
    }

    public async Task<MemberClusterCriterionDto> AddCriterionAsync(Guid clusterId, AddClusterCriterionRequest request)
    {
        var cluster = await Find(clusterId);

        var criterion = new MemberClusterCriterion
        {
            Id = Guid.NewGuid(),
            MemberClusterId = cluster.Id,
            Type = request.Type,
            UserGroupCategoryId = request.UserGroupCategoryId,
            UserGroupCategoryRoleId = request.UserGroupCategoryRoleId,
            UserGroupId = request.UserGroupId,
        };

        _db.MemberClusterCriteria.Add(criterion);
        await _db.SaveChangesAsync();

        await _db.Entry(criterion).Reference(c => c.UserGroupCategory).LoadAsync();
        await _db.Entry(criterion).Reference(c => c.UserGroupCategoryRole).LoadAsync();
        await _db.Entry(criterion).Reference(c => c.UserGroup).LoadAsync();

        return ToCriterionDto(criterion);
    }

    public async Task RemoveCriterionAsync(Guid clusterId, Guid criterionId)
    {
        var criterion = await _db.MemberClusterCriteria
            .FirstOrDefaultAsync(c => c.Id == criterionId && c.MemberClusterId == clusterId)
            ?? throw new BlueNotFoundException($"Criterion '{criterionId}' was not found on cluster '{clusterId}'.");

        _db.MemberClusterCriteria.Remove(criterion);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ClusterMemberDto>> ResolveMembersAsync(Guid clusterId)
    {
        var criteria = await _db.MemberClusterCriteria
            .Where(c => c.MemberClusterId == clusterId)
            .ToListAsync();

        if (criteria.Count == 0)
            return [];

        var currentSeasonId = await GetCurrentSeasonIdAsync();
        var userIds = new HashSet<Guid>();

        foreach (var criterion in criteria)
        {
            var ids = await ResolveUserIdsForCriterion(criterion, currentSeasonId);
            foreach (var id in ids) userIds.Add(id);
        }

        if (userIds.Count == 0)
            return [];

        return await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new ClusterMemberDto(u.Id, u.Fullname, u.Email!))
            .ToListAsync();
    }

    public async Task<bool> IsMemberAsync(Guid clusterId, Guid userId)
    {
        var criteria = await _db.MemberClusterCriteria
            .Where(c => c.MemberClusterId == clusterId)
            .ToListAsync();

        if (criteria.Count == 0)
            return false;

        var currentSeasonId = await GetCurrentSeasonIdAsync();

        foreach (var criterion in criteria)
        {
            var ids = await ResolveUserIdsForCriterion(criterion, currentSeasonId);
            if (ids.Contains(userId))
                return true;
        }

        return false;
    }

    private async Task<HashSet<Guid>> ResolveUserIdsForCriterion(MemberClusterCriterion criterion, Guid currentSeasonId)
    {
        IQueryable<Guid> query = criterion.Type switch
        {
            ClusterCriterionType.GroupCategory => _db.UserGroupInstanceMembers
                .Where(m => m.UserGroupInstance.SeasonId == currentSeasonId
                    && m.UserGroupInstance.UserGroup.UserGroupCategoryId == criterion.UserGroupCategoryId!.Value
                    && (criterion.UserGroupCategoryRoleId == null || m.UserGroupCategoryRoleId == criterion.UserGroupCategoryRoleId))
                .Select(m => m.UserId),

            ClusterCriterionType.Group => _db.UserGroupInstanceMembers
                .Where(m => m.UserGroupInstance.SeasonId == currentSeasonId
                    && m.UserGroupInstance.UserGroupId == criterion.UserGroupId!.Value)
                .Select(m => m.UserId),

            _ => throw new InvalidOperationException($"Unknown criterion type: {criterion.Type}")
        };

        return [.. await query.Distinct().ToListAsync()];
    }

    private async Task<Guid> GetCurrentSeasonIdAsync()
    {
        var settings = await _db.AppSettings.FirstAsync();
        return settings.CurrentSeasonId;
    }

    private async Task<MemberCluster> Find(Guid id)
    {
        return await _db.MemberClusters.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new BlueNotFoundException($"MemberCluster '{id}' was not found.");
    }

    private async Task<MemberCluster> FindWithCriteria(Guid id)
    {
        return await _db.MemberClusters
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroupCategory)
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroupCategoryRole)
            .Include(c => c.Criteria)
                .ThenInclude(cr => cr.UserGroup)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new BlueNotFoundException($"MemberCluster '{id}' was not found.");
    }

    private static MemberClusterDto ToDto(MemberCluster cluster) =>
        new(cluster.Id, cluster.Name, cluster.Description, cluster.Criteria.Select(ToCriterionDto).ToList());

    private static MemberClusterCriterionDto ToCriterionDto(MemberClusterCriterion c) =>
        new(c.Id, c.Type,
            c.UserGroupCategoryId, c.UserGroupCategory?.Name,
            c.UserGroupCategoryRoleId, c.UserGroupCategoryRole?.NamePlural,
            c.UserGroupId, c.UserGroup?.Name);
}
