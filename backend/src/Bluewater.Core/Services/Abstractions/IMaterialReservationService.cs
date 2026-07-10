using Bluewater.Core.Dto.MaterialPlanner;

namespace Bluewater.Core.Services.Abstractions;

public interface IMaterialReservationService
{
    Task<MaterialPlannerDayDto> GetDayAsync(DateOnly date);
    Task<MaterialReservationDto> CreateAsync(CreateMaterialReservationRequest request);
    Task<MaterialReservationDto> UpdateAsync(Guid id, UpdateMaterialReservationRequest request);
    Task DeleteAsync(Guid id);
    Task<MaterialReservationDto> SetLabelAsync(Guid id, SetMaterialReservationLabelRequest request);
}
