using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for MemberClusters — named, saved queries that resolve to a set of users,
/// used by signups, mail lists, and other features that need to target a specific audience.
/// </summary>
[ApiController]
[Route("api/member-clusters")]
public class MemberClustersController : ControllerBase
{
    private readonly IMemberClusterService _service;

    public MemberClustersController(IMemberClusterService service)
    {
        _service = service;
    }

    /// <summary>Lists all member clusters.</summary>
    [BlueAuthorize(BluePermission.ClustersView)]
    [HttpGet]
    public Task<List<MemberClusterDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Gets a single cluster by id.</summary>
    [BlueAuthorize(BluePermission.ClustersView)]
    [HttpGet("{id:guid}")]
    public Task<MemberClusterDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new member cluster.</summary>
    [BlueAuthorize(BluePermission.ClustersModify)]
    [HttpPost]
    public Task<MemberClusterDto> Create(UpsertMemberClusterRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates a cluster's name and description.</summary>
    [BlueAuthorize(BluePermission.ClustersModify)]
    [HttpPut("{id:guid}")]
    public Task<MemberClusterDto> Update(Guid id, UpsertMemberClusterRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>Deletes a cluster and all its criteria.</summary>
    [BlueAuthorize(BluePermission.ClustersModify)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }

    /// <summary>Resolves and returns the current members of a cluster.</summary>
    [BlueAuthorize(BluePermission.ClustersView)]
    [HttpGet("{id:guid}/members")]
    public Task<List<ClusterMemberDto>> Members(Guid id)
    {
        return _service.ResolveMembersAsync(id);
    }

    /// <summary>Checks whether a specific user is currently a member of the cluster.</summary>
    [BlueAuthorize(BluePermission.ClustersView)]
    [HttpGet("{id:guid}/members/{userId:guid}")]
    public async Task<IsMemberDto> IsMember(Guid id, Guid userId)
    {
        var result = await _service.IsMemberAsync(id, userId);
        return new IsMemberDto(result);
    }

    /// <summary>Adds a criterion to the cluster.</summary>
    [BlueAuthorize(BluePermission.ClustersModify)]
    [HttpPost("{id:guid}/criteria")]
    public Task<MemberClusterCriterionDto> AddCriterion(Guid id, AddClusterCriterionRequest request)
    {
        return _service.AddCriterionAsync(id, request);
    }

    /// <summary>Removes a criterion from the cluster.</summary>
    [BlueAuthorize(BluePermission.ClustersModify)]
    [HttpDelete("{id:guid}/criteria/{criterionId:guid}")]
    public Task RemoveCriterion(Guid id, Guid criterionId)
    {
        return _service.RemoveCriterionAsync(id, criterionId);
    }
}
