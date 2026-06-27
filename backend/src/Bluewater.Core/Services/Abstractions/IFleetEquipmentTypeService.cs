using Bluewater.Core.Dto.Fleet;

namespace Bluewater.Core.Services.Abstractions;

public interface IFleetEquipmentTypeService
{
    Task<List<EquipmentTypeDto>> ListAsync();
    Task<EquipmentTypeDto> GetAsync(Guid id);
    Task<EquipmentTypeDto> CreateAsync(UpsertEquipmentTypeRequest request);
    Task<EquipmentTypeDto> UpdateAsync(Guid id, UpsertEquipmentTypeRequest request);
    Task DeleteAsync(Guid id);
}
