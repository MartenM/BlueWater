using Bluewater.Domain.Models.Files;

namespace Bluewater.Infra.Services.Abstractions;

public interface IFileStorageService
{
    Task<StoredFile> StoreAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<(StoredFile Metadata, Stream Content)> RetrieveAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
