using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Fleet;

namespace Bluewater.Core.Services.Abstractions;

public interface IEquipmentService
{
    Task<PagedResult<EquipmentDto>> ListAsync(int page, int pageSize, string? search);
    Task<EquipmentDto> GetAsync(Guid id);
    Task<EquipmentDto> CreateAsync(UpsertEquipmentRequest request);
    Task<EquipmentDto> UpdateAsync(Guid id, UpsertEquipmentRequest request);
    Task DeleteAsync(Guid id);
}
