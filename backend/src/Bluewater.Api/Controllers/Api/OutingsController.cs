using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Outings;
using Bluewater.Core.Dto.Profile;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Outings;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class OutingsController : ControllerBase
{
    private readonly IOutingService _outingService;

    public OutingsController(IOutingService outingService)
    {
        _outingService = outingService;
    }

    /// <summary>Gets the current user's outings across their teams, grouped by team, soonest first.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("mine")]
    public Task<List<OutingOverviewGroupDto>> GetOverview()
    {
        return _outingService.GetOverviewAsync();
    }

    /// <summary>Gets the current user's team instances for the current season (for the outing-creation team picker).</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("my-instances")]
    public Task<List<OutingMyInstanceDto>> GetMyInstances()
    {
        return _outingService.GetMyInstancesAsync();
    }

    /// <summary>Gets outings for a team, filtered by view (upcoming / awaiting confirmation / rowed history).</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("instances/{instanceId:guid}")]
    public Task<PagedResult<OutingListItemDto>> GetForInstance(
        Guid instanceId,
        [FromQuery] OutingView view = OutingView.Upcoming,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return _outingService.GetForInstanceAsync(instanceId, view, page, pageSize);
    }

    /// <summary>Gets a single outing by id.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("{id:guid}")]
    public Task<OutingDetailDto> Get(Guid id)
    {
        return _outingService.GetAsync(id);
    }

    /// <summary>Searches for users eligible to be added to this outing's roster (instance members plus already-invited non-members).</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("{id:guid}/candidates")]
    public Task<List<ActiveMemberDto>> SearchCandidates(Guid id, [FromQuery] string? search)
    {
        return _outingService.SearchCandidatesAsync(id, search);
    }

    /// <summary>Creates a new outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPost]
    public Task<OutingDetailDto> Create(UpsertOutingRequest request)
    {
        return _outingService.CreateAsync(request);
    }

    /// <summary>Updates an outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPut("{id:guid}")]
    public Task<OutingDetailDto> Update(Guid id, UpsertOutingRequest request)
    {
        return _outingService.UpdateAsync(id, request);
    }

    /// <summary>Deletes an outing (the outing did not happen and is being removed before confirmation).</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _outingService.DeleteAsync(id);
    }

    /// <summary>Sets a participant's role on an outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPut("{id:guid}/participants/{userId:guid}/assign-role")]
    public Task<OutingDetailDto> SetParticipantRole(Guid id, Guid userId, SetParticipantRoleRequest request)
    {
        return _outingService.SetParticipantRoleAsync(id, userId, request);
    }

    /// <summary>Invites a user (not necessarily a team member) to a specific outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPost("{id:guid}/invite")]
    public Task<OutingDetailDto> Invite(Guid id, InviteParticipantRequest request)
    {
        return _outingService.InviteParticipantAsync(id, request);
    }

    /// <summary>Removes a participant from an outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public Task<OutingDetailDto> RemoveParticipant(Guid id, Guid userId)
    {
        return _outingService.RemoveParticipantAsync(id, userId);
    }

    /// <summary>Checks the current user in for an outing (within the check-in window).</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPost("{id:guid}/check-in")]
    public Task<OutingDetailDto> CheckIn(Guid id)
    {
        return _outingService.CheckInAsync(id);
    }

    /// <summary>Confirms that an outing happened as planned, freezing its roster.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPost("{id:guid}/confirm")]
    public Task<OutingDetailDto> Confirm(Guid id)
    {
        return _outingService.ConfirmAsync(id);
    }

    /// <summary>Marks an outing as not having happened, removing it.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPost("{id:guid}/did-not-happen")]
    public Task DidNotHappen(Guid id)
    {
        return _outingService.MarkDidNotHappenAsync(id);
    }

    /// <summary>Gets the changelog for an outing.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("{id:guid}/changelog")]
    public Task<List<OutingChangelogEntryDto>> GetChangelog(Guid id)
    {
        return _outingService.GetChangelogAsync(id);
    }
}
