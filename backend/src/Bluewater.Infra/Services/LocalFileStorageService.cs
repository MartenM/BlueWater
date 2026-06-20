using Bluewater.Domain.Models.Files;
using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bluewater.Infra.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IOptions<LocalFileStorageOptions> _options;

    public LocalFileStorageService(BluewaterContext db, ICurrentUserAccessor currentUserAccessor, IOptions<LocalFileStorageOptions> options)
    {
        _db = db;
        _currentUserAccessor = currentUserAccessor;
        _options = options;
    }

    public async Task<StoredFile> StoreAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = SanitizeExtension(originalFileName);
        var id = Guid.NewGuid();
        var path = BuildPath(extension, id);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var storedFile = new StoredFile
        {
            Id = id,
            OriginalFileName = originalFileName,
            Extension = extension,
            ContentType = contentType,
            SizeBytes = new FileInfo(path).Length,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = _currentUserAccessor.UserId ?? Guid.Empty
        };

        _db.StoredFiles.Add(storedFile);
        await _db.SaveChangesAsync(cancellationToken);

        return storedFile;
    }

    public async Task<(StoredFile Metadata, Stream Content)> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await Find(id, cancellationToken);
        var path = BuildPath(metadata.Extension, metadata.Id);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"StoredFile '{id}' was not found on disk.", path);
        }

        Stream content = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return (metadata, content);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await Find(id, cancellationToken);

        _db.StoredFiles.Remove(metadata);
        await _db.SaveChangesAsync(cancellationToken);

        var path = BuildPath(metadata.Extension, metadata.Id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private async Task<StoredFile> Find(Guid id, CancellationToken cancellationToken)
    {
        return await _db.StoredFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new FileNotFoundException($"StoredFile '{id}' was not found.");
    }

    private string BuildPath(string extension, Guid id) =>
        Path.Combine(_options.Value.RootPath, extension, $"{id}.{extension}");

    private static string SanitizeExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();
        return extension.Length > 0 && extension.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9')
            ? extension
            : "bin";
    }
}
