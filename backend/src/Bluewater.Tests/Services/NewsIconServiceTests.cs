using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Services.Abstractions;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class NewsIconServiceTests : SqliteServiceTestBase
{
    private readonly INewsIconService _sut;

    public NewsIconServiceTests()
    {
        _sut = GetService<INewsIconService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsActiveIcons_ExcludingSoftDeletedOnes()
    {
        var active = await _sut.CreateAsync("Active", new MemoryStream(BuildPng(100, 100)), "active.png", "image/png");
        var deleted = await _sut.CreateAsync("Deleted", new MemoryStream(BuildPng(100, 100)), "deleted.png", "image/png");
        await _sut.DeleteAsync(deleted.Id);

        var result = await _sut.ListAsync();

        result.Select(x => x.Id).ShouldBe([active.Id]);
    }

    [Fact]
    public async Task CreateAsync_PersistsIcon_AndStampsAuditFields()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;

        var result = await _sut.CreateAsync("My Icon", new MemoryStream(BuildPng(100, 100)), "icon.png", "image/png");

        result.Name.ShouldBe("My Icon");
        result.CreatedByUserId.ShouldBe(user.Id);
        (await Db.NewsIcons.CountAsync(x => x.Id == result.Id)).ShouldBe(1);

        var (metadata, content) = await GetService<IFileStorageService>().RetrieveAsync(result.FileId);
        content.Dispose();
        metadata.OriginalFileName.ShouldBe("icon.png");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameIsEmpty()
    {
        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync("   ", new MemoryStream(BuildPng(100, 100)), "icon.png", "image/png"));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenImageIsNotTheRequiredSize()
    {
        var ex = await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync("Icon", new MemoryStream(BuildPng(50, 50)), "icon.png", "image/png"));

        ex.Reason.ShouldContain("100x100");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFileIsNotPng()
    {
        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync("Icon", new MemoryStream("not an image"u8.ToArray()), "icon.png", "image/png"));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesIcon_AndLeavesTheFileOnDisk()
    {
        var icon = await _sut.CreateAsync("Icon", new MemoryStream(BuildPng(100, 100)), "icon.png", "image/png");

        await _sut.DeleteAsync(icon.Id);

        (await Db.NewsIcons.AnyAsync(x => x.Id == icon.Id)).ShouldBeFalse();
        (await Db.NewsIcons.IgnoreQueryFilters().SingleAsync(x => x.Id == icon.Id)).DeletedAt.ShouldNotBeNull();

        var (metadata, content) = await GetService<IFileStorageService>().RetrieveAsync(icon.FileId);
        content.Dispose();
        metadata.Id.ShouldBe(icon.FileId);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenIconDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetContentAsync_ReturnsContent_EvenWhenIconIsSoftDeleted()
    {
        var icon = await _sut.CreateAsync("Icon", new MemoryStream(BuildPng(100, 100)), "icon.png", "image/png");
        await _sut.DeleteAsync(icon.Id);

        var (metadata, content) = await _sut.GetContentAsync(icon.Id);
        content.Dispose();

        metadata.Id.ShouldBe(icon.FileId);
    }

    [Fact]
    public async Task GetContentAsync_Throws_WhenIconDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetContentAsync(Guid.NewGuid()));
    }

    private static byte[] BuildPng(int width, int height)
    {
        var bytes = new byte[33];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Array.Copy(signature, bytes, signature.Length);

        bytes[11] = 13; // IHDR chunk length
        bytes[12] = (byte)'I';
        bytes[13] = (byte)'H';
        bytes[14] = (byte)'D';
        bytes[15] = (byte)'R';

        WriteUInt32BigEndian(bytes, 16, width);
        WriteUInt32BigEndian(bytes, 20, height);

        return bytes;
    }

    private static void WriteUInt32BigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
