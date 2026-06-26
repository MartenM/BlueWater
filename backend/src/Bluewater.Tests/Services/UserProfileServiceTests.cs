using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services.Abstractions;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class UserProfileServiceTests : SqliteServiceTestBase
{
    private readonly IUserProfileService _sut;

    public UserProfileServiceTests()
    {
        _sut = GetService<IUserProfileService>();
    }

    [Fact]
    public async Task GetAsync_ReturnsNameAndEmptyGroups_WhenUserHasNoMemberships()
    {
        var user = await CreateUserAsync();
        user.Firstname = "Jane";
        user.SurnamePrefix = "van";
        user.Surname = "Doe";
        await UserManager.UpdateAsync(user);

        var result = await _sut.GetAsync(user.Id);

        result.Id.ShouldBe(user.Id);
        result.Firstname.ShouldBe("Jane");
        result.SurnamePrefix.ShouldBe("van");
        result.Surname.ShouldBe("Doe");
        result.Fullname.ShouldBe(user.Fullname);
        result.Groups.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_ReturnsGroupsTheUserBelongsTo()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync();

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAsync(user.Id);

        result.Groups.Count.ShouldBe(1);
        result.Groups[0].GroupId.ShouldBe(group.Id);
        result.Groups[0].GroupName.ShouldBe(group.Name);
        result.Groups[0].GroupCategoryName.ShouldBe(category.Name);
        result.Groups[0].SeasonDisplayName.ShouldBe(season.Name);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SetProfilePictureAsync_StoresFile_AndLinksItToTheUser()
    {
        var user = await CreateUserAsync();

        await _sut.SetProfilePictureAsync(user.Id, new MemoryStream(BuildPng(75, 100)), "avatar.png", "image/png");

        var updated = await Db.Users.FirstAsync(x => x.Id == user.Id);
        updated.ProfilePictureFileId.ShouldNotBeNull();

        var (metadata, content) = await GetService<IFileStorageService>().RetrieveAsync(updated.ProfilePictureFileId!.Value);
        content.Dispose();
        metadata.OriginalFileName.ShouldBe("avatar.png");
    }

    [Fact]
    public async Task SetProfilePictureAsync_DeletesThePreviousFile_WhenReplacingAnExistingPicture()
    {
        var user = await CreateUserAsync();
        await _sut.SetProfilePictureAsync(user.Id, new MemoryStream(BuildPng(75, 100)), "first.png", "image/png");
        var firstFileId = (await Db.Users.FirstAsync(x => x.Id == user.Id)).ProfilePictureFileId!.Value;

        await _sut.SetProfilePictureAsync(user.Id, new MemoryStream(BuildPng(75, 100)), "second.png", "image/png");

        var updated = await Db.Users.FirstAsync(x => x.Id == user.Id);
        updated.ProfilePictureFileId.ShouldNotBe(firstFileId);
        (await Db.StoredFiles.AnyAsync(x => x.Id == firstFileId)).ShouldBeFalse();
    }

    [Fact]
    public async Task SetProfilePictureAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() =>
            _sut.SetProfilePictureAsync(Guid.NewGuid(), new MemoryStream(BuildPng(75, 100)), "avatar.png", "image/png"));
    }

    [Fact]
    public async Task SetProfilePictureAsync_Throws_WhenImageIsNotTheRequiredSize()
    {
        var user = await CreateUserAsync();

        var ex = await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.SetProfilePictureAsync(user.Id, new MemoryStream(BuildPng(50, 50)), "avatar.png", "image/png"));

        ex.Reason.ShouldContain("75x100");
    }

    [Fact]
    public async Task SetProfilePictureAsync_Throws_WhenFileIsNotARecognizedImageFormat()
    {
        var user = await CreateUserAsync();

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.SetProfilePictureAsync(user.Id, new MemoryStream("not an image"u8.ToArray()), "avatar.png", "image/png"));
    }

    [Fact]
    public async Task GetProfilePictureAsync_ReturnsTheStoredFile_AfterItHasBeenSet()
    {
        var user = await CreateUserAsync();
        await _sut.SetProfilePictureAsync(user.Id, new MemoryStream(BuildPng(75, 100)), "avatar.png", "image/png");

        var (metadata, content) = await _sut.GetProfilePictureAsync(user.Id);
        content.Dispose();

        metadata.OriginalFileName.ShouldBe("avatar.png");
    }

    [Fact]
    public async Task GetProfilePictureAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetProfilePictureAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProfilePictureAsync_Throws_WhenUserHasNoProfilePicture()
    {
        var user = await CreateUserAsync();

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetProfilePictureAsync(user.Id));
    }

    [Fact]
    public async Task SearchActiveAsync_ReturnsEmpty_WhenNoMembersInCurrentSeason()
    {
        await CreateCurrentSeasonAsync();

        var result = await _sut.SearchActiveAsync(null);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchActiveAsync_ReturnsMembers_InCurrentSeason()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync();
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync(null);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task SearchActiveAsync_ExcludesMembers_NotInCurrentSeason()
    {
        var currentSeason = await CreateCurrentSeasonAsync();
        var otherSeason = new BlueSeason { Id = Guid.NewGuid(), StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2021, 1, 1) };
        Db.Seasons.Add(otherSeason);
        await Db.SaveChangesAsync();

        var userInCurrent = await CreateUserAsync("current", "current@example.com");
        var userInOther = await CreateUserAsync("other", "other@example.com");
        await AddMemberToSeasonAsync(userInCurrent.Id, currentSeason.Id);
        await AddMemberToSeasonAsync(userInOther.Id, otherSeason.Id);

        var result = await _sut.SearchActiveAsync(null);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(userInCurrent.Id);
    }

    [Fact]
    public async Task SearchActiveAsync_FiltersBy_Firstname()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = new BlueUser { UserName = "jan", Email = "jan@example.com", EmailConfirmed = true, Firstname = "Jan", Surname = "Bakker" };
        await UserManager.CreateAsync(user, "Test123!");
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync("jan");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task SearchActiveAsync_FiltersBy_Surname()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = new BlueUser { UserName = "piet", Email = "piet@example.com", EmailConfirmed = true, Firstname = "Piet", Surname = "Vanderberg" };
        await UserManager.CreateAsync(user, "Test123!");
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync("vanderberg");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task SearchActiveAsync_Search_IsCaseInsensitive()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = new BlueUser { UserName = "anna", Email = "anna@example.com", EmailConfirmed = true, Firstname = "Anna", Surname = "Smit" };
        await UserManager.CreateAsync(user, "Test123!");
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync("ANNA");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task SearchActiveAsync_Search_ReturnsEmpty_WhenNoMatch()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync();
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync("zzznomatch");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchActiveAsync_HasProfilePicture_IsFalse_WhenNoFileSet()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync();
        await AddMemberToSeasonAsync(user.Id, season.Id);

        var result = await _sut.SearchActiveAsync(null);

        result[0].HasProfilePicture.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchActiveAsync_DeduplicatesUser_InMultipleGroupInstances()
    {
        var season = await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync();
        await AddMemberToSeasonAsync(user.Id, season.Id, suffix: "A");
        await AddMemberToSeasonAsync(user.Id, season.Id, suffix: "B");

        var result = await _sut.SearchActiveAsync(null);

        result.Count.ShouldBe(1);
    }

    private async Task AddMemberToSeasonAsync(Guid userId, Guid seasonId, string suffix = "")
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Cat" + suffix };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Group" + suffix, UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = seasonId };
        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember
        {
            UserGroupInstanceId = instance.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.Empty
        });
        await Db.SaveChangesAsync();
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
