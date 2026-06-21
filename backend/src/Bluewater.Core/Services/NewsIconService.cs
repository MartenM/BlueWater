using System.Linq.Expressions;
using Bluewater.Core.Dto.News;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Services.Imaging;
using Bluewater.Domain.Models.Files;
using Bluewater.Domain.Models.News;
using Bluewater.Infra.Context;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class NewsIconService : INewsIconService
{
    private const int IconWidth = 100;
    private const int IconHeight = 100;

    private readonly BluewaterContext _db;
    private readonly IFileStorageService _fileStorageService;

    public NewsIconService(BluewaterContext db, IFileStorageService fileStorageService)
    {
        _db = db;
        _fileStorageService = fileStorageService;
    }

    public async Task<List<NewsIconDto>> ListAsync()
    {
        return await _db.NewsIcons
            .OrderBy(x => x.Name)
            .Select(ProjectToDto)
            .ToListAsync();
    }

    public async Task<NewsIconDto> CreateAsync(string name, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BlueValidationException("Icon name is required.");
        }

        var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        var bytes = buffered.ToArray();

        if (!PngDimensionReader.TryReadDimensions(bytes, out var size))
        {
            throw new BlueValidationException("File is not a supported image format. Only PNG is accepted.");
        }

        if (size.Width != IconWidth || size.Height != IconHeight)
        {
            throw new BlueValidationException($"News icon must be exactly {IconWidth}x{IconHeight} pixels, but was {size.Width}x{size.Height}.");
        }

        buffered.Position = 0;
        var storedFile = await _fileStorageService.StoreAsync(buffered, fileName, contentType, cancellationToken);

        var icon = new NewsIcon
        {
            Id = Guid.NewGuid(),
            Name = name,
            FileId = storedFile.Id
        };

        _db.NewsIcons.Add(icon);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(icon);
    }

    public async Task DeleteAsync(Guid id)
    {
        var icon = await _db.NewsIcons.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"NewsIcon '{id}' was not found.");

        // Soft delete only - the underlying StoredFile is left in place so posts that already
        // reference this icon keep rendering it.
        _db.NewsIcons.Remove(icon);
        await _db.SaveChangesAsync();
    }

    public async Task<(StoredFile Metadata, Stream Content)> GetContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var icon = await _db.NewsIcons.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BlueNotFoundException($"NewsIcon '{id}' was not found.");

        return await _fileStorageService.RetrieveAsync(icon.FileId, cancellationToken);
    }

    private static readonly Expression<Func<NewsIcon, NewsIconDto>> ProjectToDto =
        x => new NewsIconDto(x.Id, x.Name, x.FileId, x.CreatedAt, x.CreatedByUserId);

    private static NewsIconDto ToDto(NewsIcon icon) =>
        new(icon.Id, icon.Name, icon.FileId, icon.CreatedAt, icon.CreatedByUserId);
}
