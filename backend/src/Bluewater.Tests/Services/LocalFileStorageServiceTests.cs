using Bluewater.Infra.Services.Abstractions;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class LocalFileStorageServiceTests : SqliteServiceTestBase
{
    private readonly IFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _sut = GetService<IFileStorageService>();
    }

    [Fact]
    public async Task StoreAsync_PersistsMetadata_AndWritesFileToDisk()
    {
        var bytes = "hello world"u8.ToArray();
        var userId = (await CreateUserAsync()).Id;
        CurrentUserId = userId;

        var result = await _sut.StoreAsync(new MemoryStream(bytes), "photo.PNG", "image/png");

        result.OriginalFileName.ShouldBe("photo.PNG");
        result.Extension.ShouldBe("png");
        result.ContentType.ShouldBe("image/png");
        result.SizeBytes.ShouldBe(bytes.Length);
        result.UploadedByUserId.ShouldBe(userId);

        (await Db.StoredFiles.CountAsync(x => x.Id == result.Id)).ShouldBe(1);

        var path = Path.Combine(FileStorageRootPath, "png", $"{result.Id}.png");
        File.Exists(path).ShouldBeTrue();
        (await File.ReadAllBytesAsync(path)).ShouldBe(bytes);
    }

    [Fact]
    public async Task StoreAsync_FallsBackToBin_WhenExtensionIsMissingOrUnsafe()
    {
        var result = await _sut.StoreAsync(new MemoryStream("data"u8.ToArray()), "no-extension", "application/octet-stream");

        result.Extension.ShouldBe("bin");
        File.Exists(Path.Combine(FileStorageRootPath, "bin", $"{result.Id}.bin")).ShouldBeTrue();
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsMetadataAndContent_WhenFileExists()
    {
        var bytes = "round trip"u8.ToArray();
        var stored = await _sut.StoreAsync(new MemoryStream(bytes), "doc.txt", "text/plain");

        var (metadata, content) = await _sut.RetrieveAsync(stored.Id);
        using var reader = new MemoryStream();
        await content.CopyToAsync(reader);

        metadata.Id.ShouldBe(stored.Id);
        metadata.OriginalFileName.ShouldBe("doc.txt");
        reader.ToArray().ShouldBe(bytes);

        content.Dispose();
    }

    [Fact]
    public async Task RetrieveAsync_Throws_WhenFileDoesNotExist()
    {
        await Should.ThrowAsync<FileNotFoundException>(() => _sut.RetrieveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesDbRow_AndPhysicalFile()
    {
        var stored = await _sut.StoreAsync(new MemoryStream("bye"u8.ToArray()), "note.txt", "text/plain");
        var path = Path.Combine(FileStorageRootPath, "txt", $"{stored.Id}.txt");

        await _sut.DeleteAsync(stored.Id);

        (await Db.StoredFiles.AnyAsync(x => x.Id == stored.Id)).ShouldBeFalse();
        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenFileDoesNotExist()
    {
        await Should.ThrowAsync<FileNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }
}
