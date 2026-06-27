using Bluewater.Core.Dto.Fleet;

namespace Bluewater.Core.Services.Abstractions;

public interface IManufacturerService
{
    Task<List<ManufacturerDto>> ListAsync();
    Task<ManufacturerDto> GetAsync(Guid id);
    Task<ManufacturerDto> CreateAsync(UpsertManufacturerRequest request);
    Task<ManufacturerDto> UpdateAsync(Guid id, UpsertManufacturerRequest request);
    Task DeleteAsync(Guid id);
}
