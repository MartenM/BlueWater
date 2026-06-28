using Bluewater.Core.Dto.Clusters;

namespace Bluewater.Core.Services.Abstractions;

public interface IMemberClusterService
{
    Task<List<MemberClusterDto>> ListAsync();
    Task<MemberClusterDto> GetAsync(Guid id);
    Task<MemberClusterDto> CreateAsync(UpsertMemberClusterRequest request);
    Task<MemberClusterDto> UpdateAsync(Guid id, UpsertMemberClusterRequest request);
    Task DeleteAsync(Guid id);
    Task<MemberClusterCriterionDto> AddCriterionAsync(Guid clusterId, AddClusterCriterionRequest request);
    Task RemoveCriterionAsync(Guid clusterId, Guid criterionId);
    Task<List<ClusterMemberDto>> ResolveMembersAsync(Guid clusterId);
    Task<bool> IsMemberAsync(Guid clusterId, Guid userId);
}
