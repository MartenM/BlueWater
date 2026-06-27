using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class OarSetService : IOarSetService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertOarSetRequest> _validator;

    public OarSetService(BluewaterContext db, IValidator<UpsertOarSetRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<OarSetDto>> ListAsync()
    {
        return await _db.OarSets
            .AsNoTracking()
            .Include(x => x.Manufacturer)
            .OrderBy(x => x.Name)
            .Select(x => new OarSetDto(x.Id, x.Name, x.ManufacturerId, x.Manufacturer != null ? x.Manufacturer.Name : null, x.Scull))
            .ToListAsync();
    }

    public async Task<OarSetDto> GetAsync(Guid id)
    {
        var oarSet = await _db.OarSets
            .AsNoTracking()
            .Include(x => x.Manufacturer)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"OarSet '{id}' was not found.");

        return ToDto(oarSet);
    }

    public async Task<OarSetDto> CreateAsync(UpsertOarSetRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var oarSet = new OarSet
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ManufacturerId = request.ManufacturerId,
            Scull = request.Scull
        };

        _db.OarSets.Add(oarSet);
        await _db.SaveChangesAsync();

        return await GetAsync(oarSet.Id);
    }

    public async Task<OarSetDto> UpdateAsync(Guid id, UpsertOarSetRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var oarSet = await Find(id);
        oarSet.Name = request.Name;
        oarSet.ManufacturerId = request.ManufacturerId;
        oarSet.Scull = request.Scull;

        await _db.SaveChangesAsync();

        return await GetAsync(oarSet.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var oarSet = await Find(id);
        _db.OarSets.Remove(oarSet);
        await _db.SaveChangesAsync();
    }

    private async Task<OarSet> Find(Guid id)
    {
        return await _db.OarSets.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"OarSet '{id}' was not found.");
    }

    private static OarSetDto ToDto(OarSet x) =>
        new(x.Id, x.Name, x.ManufacturerId, x.Manufacturer?.Name, x.Scull);
}
