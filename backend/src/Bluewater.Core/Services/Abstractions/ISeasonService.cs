using Bluewater.Core.Dto.Seasons;

namespace Bluewater.Core.Services.Abstractions;

public interface ISeasonService
{
    Task<List<SeasonDto>> ListAsync();
    Task<SeasonDto> GetAsync(Guid id);
}
