using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class ManufacturerService : IManufacturerService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertManufacturerRequest> _validator;

    public ManufacturerService(BluewaterContext db, IValidator<UpsertManufacturerRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<ManufacturerDto>> ListAsync()
    {
        return await _db.Manufacturers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ManufacturerDto(x.Id, x.Name))
            .ToListAsync();
    }

    public async Task<ManufacturerDto> GetAsync(Guid id)
    {
        var manufacturer = await _db.Manufacturers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Manufacturer '{id}' was not found.");

        return new ManufacturerDto(manufacturer.Id, manufacturer.Name);
    }

    public async Task<ManufacturerDto> CreateAsync(UpsertManufacturerRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var manufacturer = new Manufacturer
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        _db.Manufacturers.Add(manufacturer);
        await _db.SaveChangesAsync();

        return await GetAsync(manufacturer.Id);
    }

    public async Task<ManufacturerDto> UpdateAsync(Guid id, UpsertManufacturerRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var manufacturer = await Find(id);
        manufacturer.Name = request.Name;

        await _db.SaveChangesAsync();

        return await GetAsync(manufacturer.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var manufacturer = await Find(id);
        _db.Manufacturers.Remove(manufacturer);
        await _db.SaveChangesAsync();
    }

    private async Task<Manufacturer> Find(Guid id)
    {
        return await _db.Manufacturers.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Manufacturer '{id}' was not found.");
    }
}
