using Bluewater.Core.Dto.MaterialPlanner;

namespace Bluewater.Core.Services.Abstractions;

public interface IMaterialReservationService
{
    Task<MaterialPlannerDayDto> GetDayAsync(DateOnly date);
    Task<MaterialReservationDto> CreateAsync(CreateMaterialReservationRequest request);
    Task<MaterialReservationDto> UpdateAsync(Guid id, UpdateMaterialReservationRequest request);
    Task DeleteAsync(Guid id);
    Task<MaterialReservationDto> SetLabelAsync(Guid id, SetMaterialReservationLabelRequest request);
    Task<MaterialReservationConflictDto> GetConflictAsync(Guid equipmentId, DateOnly date, TimeOnly startTime, TimeOnly endTime);

    /// <summary>Creates a reservation linked to an outing's boat booking. Not exposed over HTTP directly - called by OutingService.</summary>
    Task<MaterialReservationDto> CreateLinkedForOutingAsync(Guid outingId, Guid equipmentId, DateOnly date, TimeOnly startTime, TimeOnly endTime, string? label);

    /// <summary>Deletes the reservation linked to an outing, if any. Not exposed over HTTP directly - called by OutingService.</summary>
    Task DeleteLinkedForOutingAsync(Guid outingId);
}
