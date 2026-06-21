using Bluewater.Core.Dto.News;
using Bluewater.Domain.Models.Files;

namespace Bluewater.Core.Services.Abstractions;

public interface INewsIconService
{
    Task<List<NewsIconDto>> ListAsync();
    Task<NewsIconDto> CreateAsync(string name, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id);
    Task<(StoredFile Metadata, Stream Content)> GetContentAsync(Guid id, CancellationToken cancellationToken = default);
}
