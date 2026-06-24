using Bluewater.Core.Dto.Agenda;
using Bluewater.Core.Dto.Common;

namespace Bluewater.Core.Services.Abstractions;

public interface IAgendaService
{
    Task<PagedResult<AgendaItemDto>> ListAsync(int page, int pageSize);
    Task<IReadOnlyList<AgendaItemDto>> ListRangeAsync(DateOnly start, DateOnly end);
    Task<IReadOnlyList<AgendaItemDto>> ListUpcomingAsync(int count);
    Task<AgendaItemDto> GetAsync(Guid id);
    Task<AgendaItemDto> CreateAsync(UpsertAgendaItemRequest request);
    Task<AgendaItemDto> UpdateAsync(Guid id, UpsertAgendaItemRequest request);
    Task DeleteAsync(Guid id);
}
