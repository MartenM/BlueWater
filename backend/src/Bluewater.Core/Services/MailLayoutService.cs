using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MailLayoutService : IMailLayoutService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertMailLayoutRequest> _validator;

    public MailLayoutService(BluewaterContext db, IValidator<UpsertMailLayoutRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<List<MailLayoutDto>> ListAsync()
    {
        return await _db.MailLayouts
            .OrderBy(x => x.Name)
            .Select(ToDtoExpression)
            .ToListAsync();
    }

    public async Task<MailLayoutDto> GetAsync(Guid id)
    {
        return ToDto(await Find(id));
    }

    public async Task<MailLayoutDto> CreateAsync(UpsertMailLayoutRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var layout = new MailLayout
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            HeaderHtml = request.HeaderHtml,
            FooterHtml = request.FooterHtml,
            IsDefault = request.IsDefault,
        };

        _db.MailLayouts.Add(layout);
        await _db.SaveChangesAsync();

        return ToDto(layout);
    }

    public async Task<MailLayoutDto> UpdateAsync(Guid id, UpsertMailLayoutRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        var layout = await Find(id);

        layout.Name = request.Name;
        layout.HeaderHtml = request.HeaderHtml;
        layout.FooterHtml = request.FooterHtml;
        layout.IsDefault = request.IsDefault;

        await _db.SaveChangesAsync();

        return ToDto(layout);
    }

    public async Task DeleteAsync(Guid id)
    {
        var layout = await Find(id);
        _db.MailLayouts.Remove(layout);
        await _db.SaveChangesAsync();
    }

    private async Task<MailLayout> Find(Guid id)
    {
        return await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"MailLayout '{id}' was not found.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<MailLayout, MailLayoutDto>> ToDtoExpression =
        x => new MailLayoutDto(x.Id, x.Name, x.HeaderHtml, x.FooterHtml, x.IsDefault);

    private static MailLayoutDto ToDto(MailLayout layout) =>
        new(layout.Id, layout.Name, layout.HeaderHtml, layout.FooterHtml, layout.IsDefault);
}
