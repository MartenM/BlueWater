using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Outings;
using Bluewater.Domain.Models.Outings;

namespace Bluewater.Core.Services.Abstractions;

public interface IOutingService
{
    Task<List<OutingOverviewGroupDto>> GetOverviewAsync();
    Task<List<OutingMyInstanceDto>> GetMyInstancesAsync();
    Task<PagedResult<OutingListItemDto>> GetForInstanceAsync(Guid instanceId, OutingView view, int page, int pageSize);
    Task<OutingDetailDto> GetAsync(Guid outingId);
    Task<OutingDetailDto> CreateAsync(UpsertOutingRequest request);
    Task<OutingDetailDto> UpdateAsync(Guid outingId, UpsertOutingRequest request);
    Task DeleteAsync(Guid outingId);
    Task<OutingDetailDto> SetParticipantRoleAsync(Guid outingId, Guid userId, SetParticipantRoleRequest request);
    Task<OutingDetailDto> InviteParticipantAsync(Guid outingId, InviteParticipantRequest request);
    Task<OutingDetailDto> RemoveParticipantAsync(Guid outingId, Guid userId);
    Task<OutingDetailDto> CheckInAsync(Guid outingId);
    Task<OutingDetailDto> ConfirmAsync(Guid outingId);
    Task MarkDidNotHappenAsync(Guid outingId);
    Task<List<OutingChangelogEntryDto>> GetChangelogAsync(Guid outingId);
}
