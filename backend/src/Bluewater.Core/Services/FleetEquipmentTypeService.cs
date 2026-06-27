using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class FleetEquipmentTypeService : IFleetEquipmentTypeService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertEquipmentTypeRequest> _validator;

    public FleetEquipmentTypeService(BluewaterContext db, IValidator<UpsertEquipmentTypeRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<EquipmentTypeDto>> ListAsync()
    {
        return await _db.EquipmentTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new EquipmentTypeDto(x.Id, x.Code, x.Name, x.Scull, x.Coxed, x.RowersCount, x.IsBoat))
            .ToListAsync();
    }

    public async Task<EquipmentTypeDto> GetAsync(Guid id)
    {
        var type = await _db.EquipmentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"EquipmentType '{id}' was not found.");

        return ToDto(type);
    }

    public async Task<EquipmentTypeDto> CreateAsync(UpsertEquipmentTypeRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var type = new EquipmentType
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Scull = request.Scull,
            Coxed = request.Coxed,
            RowersCount = request.RowersCount,
            IsBoat = request.IsBoat
        };

        _db.EquipmentTypes.Add(type);
        await _db.SaveChangesAsync();

        return await GetAsync(type.Id);
    }

    public async Task<EquipmentTypeDto> UpdateAsync(Guid id, UpsertEquipmentTypeRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var type = await Find(id);
        type.Code = request.Code;
        type.Name = request.Name;
        type.Scull = request.Scull;
        type.Coxed = request.Coxed;
        type.RowersCount = request.RowersCount;
        type.IsBoat = request.IsBoat;

        await _db.SaveChangesAsync();

        return await GetAsync(type.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var type = await Find(id);
        _db.EquipmentTypes.Remove(type);
        await _db.SaveChangesAsync();
    }

    private async Task<EquipmentType> Find(Guid id)
    {
        return await _db.EquipmentTypes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"EquipmentType '{id}' was not found.");
    }

    private static EquipmentTypeDto ToDto(EquipmentType x) =>
        new(x.Id, x.Code, x.Name, x.Scull, x.Coxed, x.RowersCount, x.IsBoat);
}
