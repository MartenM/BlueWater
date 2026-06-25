using Bluewater.Core.Dto.Profile;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Services.Imaging;
using Bluewater.Domain.Models.Files;
using Bluewater.Infra.Context;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserProfileService : IUserProfileService
{
    private const int ProfilePictureWidth = 75;
    private const int ProfilePictureHeight = 100;

    private readonly BluewaterContext _db;
    private readonly IUserGroupMembershipService _membershipService;
    private readonly IFileStorageService _fileStorageService;

    public UserProfileService(BluewaterContext db, IUserGroupMembershipService membershipService, IFileStorageService fileStorageService)
    {
        _db = db;
        _membershipService = membershipService;
        _fileStorageService = fileStorageService;
    }

    public async Task<UserProfileDto> GetAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new BlueNotFoundException($"User '{userId}' was not found.");

        var groups = await _membershipService.GetGroupsForUserAsync(userId);

        return new UserProfileDto(user.Id, user.Firstname, user.SurnamePrefix, user.Surname, user.Fullname, groups);
    }

    public async Task SetProfilePictureAsync(Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BlueNotFoundException($"User '{userId}' was not found.");

        var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        var bytes = buffered.ToArray();

        var (width, height) = ReadImageDimensions(bytes);
        if (width != ProfilePictureWidth || height != ProfilePictureHeight)
        {
            throw new BlueValidationException($"Profile picture must be exactly {ProfilePictureWidth}x{ProfilePictureHeight} pixels, but was {width}x{height}.");
        }

        var previousFileId = user.ProfilePictureFileId;

        buffered.Position = 0;
        var storedFile = await _fileStorageService.StoreAsync(buffered, fileName, contentType, cancellationToken);

        user.ProfilePictureFileId = storedFile.Id;
        await _db.SaveChangesAsync(cancellationToken);

        if (previousFileId.HasValue)
        {
            await _fileStorageService.DeleteAsync(previousFileId.Value, cancellationToken);
        }
    }

    public async Task<(StoredFile Metadata, Stream Content)> GetProfilePictureAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BlueNotFoundException($"User '{userId}' was not found.");

        if (user.ProfilePictureFileId is not Guid fileId)
        {
            throw new BlueNotFoundException($"User '{userId}' has no profile picture.");
        }

        return await _fileStorageService.RetrieveAsync(fileId, cancellationToken);
    }

    /// <summary>
    /// Reads width/height directly from PNG/JPEG headers rather than pulling in a full image
    /// library, which avoids the licensing requirements that the common .NET imaging packages
    /// (e.g. SixLabors.ImageSharp v3+) impose for commercial use.
    /// </summary>
    private static (int Width, int Height) ReadImageDimensions(byte[] bytes)
    {
        if (PngDimensionReader.TryReadDimensions(bytes, out var pngSize))
        {
            return pngSize;
        }

        if (TryReadJpegDimensions(bytes, out var jpegSize))
        {
            return jpegSize;
        }

        throw new BlueValidationException("File is not a supported image format. Only PNG and JPEG are accepted.");
    }

    private static bool TryReadJpegDimensions(byte[] bytes, out (int Width, int Height) size)
    {
        size = default;

        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        var i = 2;
        while (i + 9 <= bytes.Length && bytes[i] == 0xFF)
        {
            var marker = bytes[i + 1];

            // Standalone markers (no length/payload): SOI, TEM, RSTn.
            if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
            {
                i += 2;
                continue;
            }

            if (marker == 0xD9) // EOI
            {
                break;
            }

            var length = ReadUInt16BigEndian(bytes, i + 2);
            var isStartOfFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
            if (isStartOfFrame)
            {
                size = (ReadUInt16BigEndian(bytes, i + 7), ReadUInt16BigEndian(bytes, i + 5));
                return true;
            }

            i += 2 + length;
        }

        return false;
    }

    private static int ReadUInt16BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 8) | bytes[offset + 1];
}
