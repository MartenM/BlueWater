using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.News;

namespace Bluewater.Core.Services.Abstractions;

public interface INewsService
{
    Task<PagedResult<NewsPostDto>> ListAsync(int page, int pageSize);
    Task<NewsPostDto> GetAsync(Guid id);
    Task<NewsPostDto> CreateAsync(UpsertNewsPostRequest request);
    Task<NewsPostDto> UpdateAsync(Guid id, UpsertNewsPostRequest request);
    Task DeleteAsync(Guid id);
}
