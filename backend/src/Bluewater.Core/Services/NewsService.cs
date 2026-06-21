using System.Linq.Expressions;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.News;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.News;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class NewsService : INewsService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserService _currentUser;

    public NewsService(BluewaterContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NewsPostDto>> ListAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Visible(_db.NewsPosts);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync();

        return new PagedResult<NewsPostDto>(items, page, pageSize, totalCount);
    }

    public async Task<NewsPostDto> GetAsync(Guid id)
    {
        return await Visible(_db.NewsPosts)
            .Where(x => x.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync()
            ?? throw new BlueNotFoundException($"NewsPost '{id}' was not found.");
    }

    public async Task<NewsPostDto> CreateAsync(UpsertNewsPostRequest request)
    {
        var post = new NewsPost
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ShortText = request.ShortText,
            AdditionalText = request.AdditionalText,
            MembersOnly = request.MembersOnly
        };

        _db.NewsPosts.Add(post);
        await _db.SaveChangesAsync();

        return ToDto(post);
    }

    public async Task<NewsPostDto> UpdateAsync(Guid id, UpsertNewsPostRequest request)
    {
        var post = await Find(id);

        post.Title = request.Title;
        post.ShortText = request.ShortText;
        post.AdditionalText = request.AdditionalText;
        post.MembersOnly = request.MembersOnly;

        await _db.SaveChangesAsync();

        return ToDto(post);
    }

    public async Task DeleteAsync(Guid id)
    {
        var post = await Find(id);

        _db.NewsPosts.Remove(post);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Members-only posts are hidden from anyone without the NewsView permission - including
    /// anonymous callers, since List/Get are open endpoints that decide visibility in-service.
    /// </summary>
    private IQueryable<NewsPost> Visible(IQueryable<NewsPost> query) =>
        _currentUser.HasPermission(BluePermission.NewsView) ? query : query.Where(x => !x.MembersOnly);

    private async Task<NewsPost> Find(Guid id)
    {
        return await _db.NewsPosts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"NewsPost '{id}' was not found.");
    }

    private static readonly Expression<Func<NewsPost, NewsPostDto>> ProjectToDto =
        x => new NewsPostDto(x.Id, x.Title, x.ShortText, x.AdditionalText, x.MembersOnly,
            x.CreatedAt, x.CreatedByUserId, x.UpdatedAt, x.UpdatedByUserId);

    private static NewsPostDto ToDto(NewsPost post) =>
        new(post.Id, post.Title, post.ShortText, post.AdditionalText, post.MembersOnly,
            post.CreatedAt, post.CreatedByUserId, post.UpdatedAt, post.UpdatedByUserId);
}
