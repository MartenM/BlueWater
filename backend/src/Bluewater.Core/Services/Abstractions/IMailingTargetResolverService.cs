using Bluewater.Core.Dto.Clusters;

namespace Bluewater.Core.Services.Abstractions;

public interface IMailingTargetResolverService
{
    Task<List<ClusterMemberDto>> ResolveRecipientsAsync(Guid mailingId);
}
