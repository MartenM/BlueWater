using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class EquipmentService : IEquipmentService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertEquipmentRequest> _validator;

    public EquipmentService(BluewaterContext db, IValidator<UpsertEquipmentRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<EquipmentDto>> ListAsync(int page, int pageSize, string? search)
    {
        var query = _db.Equipment
            .AsNoTracking()
            .Include(x => x.EquipmentType)
            .Include(x => x.Manufacturer)
            .Include(x => x.OarSet)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x))
            .ToListAsync();

        return new PagedResult<EquipmentDto>(items, page, pageSize, totalCount);
    }

    public async Task<EquipmentDto> GetAsync(Guid id)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .Include(x => x.EquipmentType)
            .Include(x => x.Manufacturer)
            .Include(x => x.OarSet)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Equipment '{id}' was not found.");

        return ToDto(equipment);
    }

    public async Task<EquipmentDto> CreateAsync(UpsertEquipmentRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            EquipmentTypeId = request.EquipmentTypeId,
            ManufacturerId = request.ManufacturerId,
            OarSetId = request.OarSetId,
            FreeFleet = request.FreeFleet,
            OutOfOrder = request.OutOfOrder,
            RowersWeight = request.RowersWeight,
            RowersWeightMax = request.RowersWeightMax,
            DateBuild = request.DateBuild,
            DateBought = request.DateBought,
            DateSold = request.DateSold
        };

        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync();

        return await GetAsync(equipment.Id);
    }

    public async Task<EquipmentDto> UpdateAsync(Guid id, UpsertEquipmentRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var equipment = await Find(id);
        equipment.Name = request.Name;
        equipment.Description = request.Description;
        equipment.EquipmentTypeId = request.EquipmentTypeId;
        equipment.ManufacturerId = request.ManufacturerId;
        equipment.OarSetId = request.OarSetId;
        equipment.FreeFleet = request.FreeFleet;
        equipment.OutOfOrder = request.OutOfOrder;
        equipment.RowersWeight = request.RowersWeight;
        equipment.RowersWeightMax = request.RowersWeightMax;
        equipment.DateBuild = request.DateBuild;
        equipment.DateBought = request.DateBought;
        equipment.DateSold = request.DateSold;

        await _db.SaveChangesAsync();

        return await GetAsync(equipment.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var equipment = await Find(id);
        _db.Equipment.Remove(equipment);
        await _db.SaveChangesAsync();
    }

    private async Task<Equipment> Find(Guid id)
    {
        return await _db.Equipment.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Equipment '{id}' was not found.");
    }

    private static EquipmentDto ToDto(Equipment x) => new(
        x.Id, x.Name, x.Description,
        x.EquipmentTypeId, x.EquipmentType?.Name,
        x.ManufacturerId, x.Manufacturer?.Name,
        x.OarSetId, x.OarSet?.Name,
        x.FreeFleet, x.OutOfOrder,
        x.RowersWeight, x.RowersWeightMax,
        x.DateBuild, x.DateBought, x.DateSold);
}
