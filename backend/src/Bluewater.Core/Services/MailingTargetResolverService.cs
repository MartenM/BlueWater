using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

/// <summary>
/// Resolves a Mailing's target clusters and target group instances into the union of their
/// current members, deduplicated by user. Reuses <see cref="IMemberClusterService.ResolveMembersAsync"/>
/// directly for cluster targeting rather than reimplementing criteria resolution.
/// </summary>
public class MailingTargetResolverService : IMailingTargetResolverService
{
    private readonly BluewaterContext _db;
    private readonly IMemberClusterService _clusterService;

    public MailingTargetResolverService(BluewaterContext db, IMemberClusterService clusterService)
    {
        _db = db;
        _clusterService = clusterService;
    }

    public async Task<List<ClusterMemberDto>> ResolveRecipientsAsync(Guid mailingId)
    {
        var targetClusterIds = await _db.MailingTargetClusters
            .Where(x => x.MailingId == mailingId)
            .Select(x => x.MemberClusterId)
            .ToListAsync();

        var targetGroupInstanceIds = await _db.MailingTargetGroupInstances
            .Where(x => x.MailingId == mailingId)
            .Select(x => x.UserGroupInstanceId)
            .ToListAsync();

        var members = new Dictionary<Guid, ClusterMemberDto>();

        foreach (var clusterId in targetClusterIds)
        {
            foreach (var member in await _clusterService.ResolveMembersAsync(clusterId))
            {
                members[member.UserId] = member;
            }
        }

        foreach (var instanceId in targetGroupInstanceIds)
        {
            var instanceMembers = await _db.UserGroupInstanceMembers
                .Where(m => m.UserGroupInstanceId == instanceId)
                .Select(m => new ClusterMemberDto(m.UserId, m.User.Fullname, m.User.Email!))
                .ToListAsync();

            foreach (var member in instanceMembers)
            {
                members[member.UserId] = member;
            }
        }

        return members.Values
            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
            .ToList();
    }
}
