using Bluewater.Core.Dto.Seasons;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class SeasonService : ISeasonService
{
    private readonly BluewaterContext _db;

    public SeasonService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<List<SeasonDto>> ListAsync()
    {
        var currentSeasonId = await GetCurrentSeasonId();

        var seasons = await _db.Seasons
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        return seasons
            .Select(x => ToDto(x, currentSeasonId))
            .ToList();
    }

    public async Task<SeasonDto> GetAsync(Guid id)
    {
        var season = await _db.Seasons.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Season '{id}' was not found.");

        var currentSeasonId = await GetCurrentSeasonId();

        return ToDto(season, currentSeasonId);
    }

    private Task<Guid?> GetCurrentSeasonId()
    {
        return _db.AppSettings
            .Select(x => (Guid?)x.CurrentSeasonId)
            .FirstOrDefaultAsync();
    }

    private static SeasonDto ToDto(BlueSeason x, Guid? currentSeasonId) =>
        new(x.Id, x.Name, x.StartDate, x.EndDate, x.Id == currentSeasonId);
}
