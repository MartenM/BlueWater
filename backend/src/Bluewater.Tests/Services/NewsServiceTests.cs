using Bluewater.Core.Dto.News;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.News;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class NewsServiceTests : SqliteServiceTestBase
{
    private readonly INewsService _sut;

    public NewsServiceTests()
    {
        _sut = GetService<INewsService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoPostsExist()
    {
        var result = await _sut.ListAsync(1, 20);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_OrdersNewestFirst_AndPaginates()
    {
        var older = await AddPostAsync("Older", createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = await AddPostAsync("Newer", createdAt: DateTime.UtcNow.AddDays(-1));
        var newest = await AddPostAsync("Newest", createdAt: DateTime.UtcNow);

        var page1 = await _sut.ListAsync(1, 2);
        page1.Items.Select(x => x.Id).ShouldBe([newest.Id, newer.Id]);
        page1.TotalCount.ShouldBe(3);

        var page2 = await _sut.ListAsync(2, 2);
        page2.Items.Select(x => x.Id).ShouldBe([older.Id]);
    }

    [Fact]
    public async Task ListAsync_HidesMembersOnlyPosts_WithoutNewsView()
    {
        await AddPostAsync("Public", membersOnly: false);
        await AddPostAsync("Members", membersOnly: true);

        var result = await _sut.ListAsync(1, 20);

        result.Items.Count.ShouldBe(1);
        result.Items.Single().Title.ShouldBe("Public");
    }

    [Fact]
    public async Task ListAsync_IncludesMembersOnlyPosts_WithNewsView()
    {
        await AddPostAsync("Public", membersOnly: false);
        await AddPostAsync("Members", membersOnly: true);
        CurrentUserPermissions.Add(BluePermission.NewsView);

        var result = await _sut.ListAsync(1, 20);

        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAsync_ReturnsPost_WhenItExistsAndIsPublic()
    {
        var post = await AddPostAsync("Public", membersOnly: false);

        var result = await _sut.GetAsync(post.Id);

        result.Title.ShouldBe("Public");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenPostDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenPostIsMembersOnly_AndCallerLacksNewsView()
    {
        var post = await AddPostAsync("Members", membersOnly: true);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(post.Id));
    }

    [Fact]
    public async Task GetAsync_ReturnsPost_WhenMembersOnly_AndCallerHasNewsView()
    {
        var post = await AddPostAsync("Members", membersOnly: true);
        CurrentUserPermissions.Add(BluePermission.NewsView);

        var result = await _sut.GetAsync(post.Id);

        result.Title.ShouldBe("Members");
    }

    [Fact]
    public async Task CreateAsync_PersistsPost_AndStampsAuditFields()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var request = new UpsertNewsPostRequest("Title", "Short", "Additional", true);

        var result = await _sut.CreateAsync(request);

        result.Title.ShouldBe(request.Title);
        result.ShortText.ShouldBe(request.ShortText);
        result.AdditionalText.ShouldBe(request.AdditionalText);
        result.MembersOnly.ShouldBeTrue();
        result.CreatedByUserId.ShouldBe(user.Id);
        (await Db.NewsPosts.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingPost_AndStampsAuditFields()
    {
        var post = await AddPostAsync("Original");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;

        var result = await _sut.UpdateAsync(post.Id, new UpsertNewsPostRequest("Renamed", "New short", null, true));

        result.Title.ShouldBe("Renamed");
        result.ShortText.ShouldBe("New short");
        result.AdditionalText.ShouldBeNull();
        result.MembersOnly.ShouldBeTrue();
        result.UpdatedByUserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenPostDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertNewsPostRequest("Title", "Short", null, false)));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesPost()
    {
        var post = await AddPostAsync("Title");

        await _sut.DeleteAsync(post.Id);

        (await Db.NewsPosts.AnyAsync(x => x.Id == post.Id)).ShouldBeFalse();
        (await Db.NewsPosts.IgnoreQueryFilters().SingleAsync(x => x.Id == post.Id)).DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenPostDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private async Task<NewsPost> AddPostAsync(string title, bool membersOnly = false, DateTime? createdAt = null)
    {
        var post = new NewsPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortText = "Short text",
            MembersOnly = membersOnly
        };
        Db.NewsPosts.Add(post);
        await Db.SaveChangesAsync();

        if (createdAt is { } explicitCreatedAt)
        {
            post.CreatedAt = explicitCreatedAt;
            await Db.SaveChangesAsync();
        }

        return post;
    }
}
