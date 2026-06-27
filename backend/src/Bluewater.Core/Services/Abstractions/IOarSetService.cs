using Bluewater.Core.Dto.Fleet;

namespace Bluewater.Core.Services.Abstractions;

public interface IOarSetService
{
    Task<List<OarSetDto>> ListAsync();
    Task<OarSetDto> GetAsync(Guid id);
    Task<OarSetDto> CreateAsync(UpsertOarSetRequest request);
    Task<OarSetDto> UpdateAsync(Guid id, UpsertOarSetRequest request);
    Task DeleteAsync(Guid id);
}
