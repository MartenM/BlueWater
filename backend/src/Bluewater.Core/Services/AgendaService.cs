using System.Linq.Expressions;
using Bluewater.Core.Dto.Agenda;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Agenda;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class AgendaService : IAgendaService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertAgendaItemRequest> _validator;

    public AgendaService(BluewaterContext db, IValidator<UpsertAgendaItemRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<AgendaItemDto>> ListAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.AgendaItems.AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync();

        return new PagedResult<AgendaItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<AgendaItemDto>> ListRangeAsync(DateOnly start, DateOnly end)
    {
        return await _db.AgendaItems
            .Where(x => x.Date <= end && (x.EndDate ?? x.Date) >= start)
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .Select(ProjectToDto)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AgendaItemDto>> ListUpcomingAsync(int count)
    {
        count = Math.Max(count, 1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.AgendaItems
            .Where(x => (x.EndDate ?? x.Date) >= today)
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .Take(count)
            .Select(ProjectToDto)
            .ToListAsync();
    }

    public async Task<AgendaItemDto> GetAsync(Guid id)
    {
        return await _db.AgendaItems
            .Where(x => x.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync()
            ?? throw new BlueNotFoundException($"AgendaItem '{id}' was not found.");
    }

    public async Task<AgendaItemDto> CreateAsync(UpsertAgendaItemRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var item = new AgendaItem
        {
            Id = Guid.NewGuid(),
            Date = request.Date,
            Time = request.Time,
            Title = request.Title,
            Description = request.Description,
            EndDate = request.EndDate,
            EndTime = request.EndTime
        };

        _db.AgendaItems.Add(item);
        await _db.SaveChangesAsync();

        return ToDto(item);
    }

    public async Task<AgendaItemDto> UpdateAsync(Guid id, UpsertAgendaItemRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        var item = await Find(id);

        item.Date = request.Date;
        item.Time = request.Time;
        item.Title = request.Title;
        item.Description = request.Description;
        item.EndDate = request.EndDate;
        item.EndTime = request.EndTime;

        await _db.SaveChangesAsync();

        return ToDto(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await Find(id);

        _db.AgendaItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    private async Task<AgendaItem> Find(Guid id)
    {
        return await _db.AgendaItems.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"AgendaItem '{id}' was not found.");
    }

    private static readonly Expression<Func<AgendaItem, AgendaItemDto>> ProjectToDto =
        x => new AgendaItemDto(x.Id, x.Date, x.Time, x.Title, x.Description, x.EndDate, x.EndTime,
            x.CreatedAt, x.CreatedByUserId, x.UpdatedAt, x.UpdatedByUserId);

    private static AgendaItemDto ToDto(AgendaItem item) =>
        new(item.Id, item.Date, item.Time, item.Title, item.Description, item.EndDate, item.EndTime,
            item.CreatedAt, item.CreatedByUserId, item.UpdatedAt, item.UpdatedByUserId);
}
