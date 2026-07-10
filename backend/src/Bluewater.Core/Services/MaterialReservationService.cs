using Bluewater.Core.Dto.MaterialPlanner;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.MaterialPlanner;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MaterialReservationService : IMaterialReservationService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateMaterialReservationRequest> _createValidator;
    private readonly IValidator<UpdateMaterialReservationRequest> _updateValidator;
    private readonly IValidator<SetMaterialReservationLabelRequest> _labelValidator;

    public MaterialReservationService(
        BluewaterContext db,
        ICurrentUserService currentUser,
        IValidator<CreateMaterialReservationRequest> createValidator,
        IValidator<UpdateMaterialReservationRequest> updateValidator,
        IValidator<SetMaterialReservationLabelRequest> labelValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _labelValidator = labelValidator;
    }

    public async Task<MaterialPlannerDayDto> GetDayAsync(DateOnly date)
    {
        var boats = await _db.Equipment
            .AsNoTracking()
            .Where(e => e.FreeFleet && !e.OutOfOrder)
            .Include(e => e.EquipmentType)
            .ToListAsync();

        var boatIds = boats.Select(b => b.Id).ToList();

        var reservations = await _db.MaterialReservations
            .AsNoTracking()
            .Where(r => r.Date == date && boatIds.Contains(r.EquipmentId))
            .ToListAsync();

        var ownerIds = reservations.Select(r => r.CreatedByUserId).Distinct().ToList();
        var owners = await _db.Users
            .AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var reservationsByEquipment = reservations
            .GroupBy(r => r.EquipmentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.StartTime).Select(r => ToDto(r, owners)).ToList());

        var boatTypeGroups = boats
            .GroupBy(e => (e.EquipmentTypeId, TypeLabel: e.EquipmentType?.Name ?? "Overig"))
            .OrderBy(g => g.Key.TypeLabel)
            .Select(g => new MaterialPlannerBoatTypeGroupDto(
                g.Key.EquipmentTypeId,
                g.Key.TypeLabel,
                g.OrderBy(e => e.Name)
                    .Select(e => new MaterialPlannerBoatDto(
                        e.Id,
                        e.Name,
                        reservationsByEquipment.GetValueOrDefault(e.Id, [])))
                    .ToList()))
            .ToList();

        return new MaterialPlannerDayDto(date, boatTypeGroups);
    }

    public async Task<MaterialReservationDto> CreateAsync(CreateMaterialReservationRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        await AssertNoConflictAsync(request.EquipmentId, request.Date, request.StartTime, request.EndTime, excludeId: null);

        var reservation = new MaterialReservation
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
        };
        _db.MaterialReservations.Add(reservation);
        await _db.SaveChangesAsync();

        return await ToDtoWithOwnerAsync(reservation);
    }

    public async Task<MaterialReservationDto> UpdateAsync(Guid id, UpdateMaterialReservationRequest request)
    {
        await _updateValidator.ValidateAndThrowAsync(request);

        var reservation = await _db.MaterialReservations.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new BlueNotFoundException($"Material reservation '{id}' was not found.");

        AssertOwnerOrOverride(reservation);

        await AssertNoConflictAsync(reservation.EquipmentId, reservation.Date, request.StartTime, request.EndTime, excludeId: reservation.Id);

        reservation.StartTime = request.StartTime;
        reservation.EndTime = request.EndTime;
        await _db.SaveChangesAsync();

        return await ToDtoWithOwnerAsync(reservation);
    }

    public async Task DeleteAsync(Guid id)
    {
        var reservation = await _db.MaterialReservations.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new BlueNotFoundException($"Material reservation '{id}' was not found.");

        AssertOwnerOrOverride(reservation);

        _db.MaterialReservations.Remove(reservation);
        await _db.SaveChangesAsync();
    }

    public async Task<MaterialReservationDto> SetLabelAsync(Guid id, SetMaterialReservationLabelRequest request)
    {
        await _labelValidator.ValidateAndThrowAsync(request);

        var reservation = await _db.MaterialReservations.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new BlueNotFoundException($"Material reservation '{id}' was not found.");

        AssertOwnerOrOverride(reservation);

        reservation.CustomLabel = string.IsNullOrWhiteSpace(request.CustomLabel) ? null : request.CustomLabel;
        await _db.SaveChangesAsync();

        return await ToDtoWithOwnerAsync(reservation);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task AssertNoConflictAsync(Guid equipmentId, DateOnly date, TimeOnly start, TimeOnly end, Guid? excludeId)
    {
        var hasConflict = await _db.MaterialReservations
            .Where(r => r.EquipmentId == equipmentId && r.Date == date)
            .Where(r => excludeId == null || r.Id != excludeId)
            .AnyAsync(r => r.StartTime < end && r.EndTime > start);

        if (hasConflict)
            throw new BlueValidationException("This boat is already reserved for part of that time range.");
    }

    private void AssertOwnerOrOverride(MaterialReservation reservation)
    {
        if (reservation.CreatedByUserId != _currentUser.Id && !_currentUser.HasPermission(BluePermission.MaterialPlannerOverride))
            throw new BlueValidationException("You can only edit or delete your own reservations.");
    }

    private async Task<MaterialReservationDto> ToDtoWithOwnerAsync(MaterialReservation reservation)
    {
        var owner = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == reservation.CreatedByUserId);
        return ToDto(reservation, new Dictionary<Guid, Domain.Models.BlueUser> { [owner.Id] = owner });
    }

    private MaterialReservationDto ToDto(MaterialReservation reservation, IReadOnlyDictionary<Guid, Domain.Models.BlueUser> owners)
    {
        var ownerFullname = owners.TryGetValue(reservation.CreatedByUserId, out var owner) ? owner.Fullname : string.Empty;
        var canEdit = reservation.CreatedByUserId == _currentUser.Id || _currentUser.HasPermission(BluePermission.MaterialPlannerOverride);

        return new MaterialReservationDto(
            reservation.Id,
            reservation.EquipmentId,
            reservation.Date,
            reservation.StartTime,
            reservation.EndTime,
            reservation.CreatedByUserId,
            ownerFullname,
            reservation.CustomLabel,
            canEdit);
    }
}
