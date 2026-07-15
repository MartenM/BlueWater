using AngleSharp.Html.Parser;
using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Services.Mail;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services.Abstractions;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bluewater.Core.Services;

public class MailingService : IMailingService
{
    private static readonly IReadOnlyDictionary<string, string> SampleTokenValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["FirstName"] = "Jane",
        ["FullName"] = "Jane Doe",
        ["Email"] = "jane.doe@example.com",
        ["FormalSalutation"] = "Dear Jane Doe",
        ["AddressBlock"] = "Samplestraat 1, 1234 AB, Sample City",
    };

    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertMailingRequest> _validator;
    private readonly IMailContentRenderer _renderer;
    private readonly IMailingTargetResolverService _targetResolver;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly MailOptions _mailOptions;

    public MailingService(
        BluewaterContext db,
        IValidator<UpsertMailingRequest> validator,
        IMailContentRenderer renderer,
        IMailingTargetResolverService targetResolver,
        IBackgroundJobClient backgroundJobClient,
        ICurrentUserAccessor currentUserAccessor,
        IOptions<MailOptions> mailOptions)
    {
        _db = db;
        _validator = validator;
        _renderer = renderer;
        _targetResolver = targetResolver;
        _backgroundJobClient = backgroundJobClient;
        _currentUserAccessor = currentUserAccessor;
        _mailOptions = mailOptions.Value;
    }

    public async Task<List<MailingDto>> ListAsync()
    {
        var mailings = await FindQueryWithTargets().ToListAsync();
        return mailings.Select(ToDto).ToList();
    }

    public async Task<MailingDto> GetAsync(Guid id)
    {
        return ToDto(await FindWithTargets(id));
    }

    public async Task<MailingDto> CreateAsync(UpsertMailingRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var mailing = new Mailing
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            BodyMarkdown = request.BodyMarkdown,
            SenderKey = request.SenderKey,
            TemplateId = request.TemplateId,
            LayoutId = request.LayoutId,
            Status = MailingStatus.Draft,
        };

        _db.Mailings.Add(mailing);
        await _db.SaveChangesAsync();

        return ToDto(mailing);
    }

    public async Task<MailingDto> UpdateAsync(Guid id, UpsertMailingRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        var mailing = await FindWithTargets(id);
        EnsureDraft(mailing);

        mailing.Subject = request.Subject;
        mailing.BodyMarkdown = request.BodyMarkdown;
        mailing.SenderKey = request.SenderKey;
        mailing.TemplateId = request.TemplateId;
        mailing.LayoutId = request.LayoutId;

        await _db.SaveChangesAsync();

        return ToDto(mailing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var mailing = await Find(id);
        EnsureDraft(mailing);

        _db.Mailings.Remove(mailing);
        await _db.SaveChangesAsync();
    }

    public async Task<MailingDto> AddTargetClusterAsync(Guid mailingId, Guid clusterId)
    {
        var mailing = await Find(mailingId);

        var exists = await _db.MailingTargetClusters.AnyAsync(x => x.MailingId == mailingId && x.MemberClusterId == clusterId);
        if (!exists)
        {
            _db.MailingTargetClusters.Add(new MailingTargetCluster { MailingId = mailingId, MemberClusterId = clusterId });
            await _db.SaveChangesAsync();
        }

        // AsNoTracking: avoids marking TargetClusters "loaded" on the tracked Mailing instance,
        // which would otherwise leave a stale entry behind if that target is later removed within
        // the same DbContext scope (soft-delete + detach doesn't retroactively prune an
        // already-materialized collection navigation).
        return ToDto(await FindQueryWithTargets().AsNoTracking().FirstAsync(x => x.Id == mailingId));
    }

    public async Task RemoveTargetClusterAsync(Guid mailingId, Guid clusterId)
    {
        var target = await _db.MailingTargetClusters
            .FirstOrDefaultAsync(x => x.MailingId == mailingId && x.MemberClusterId == clusterId)
            ?? throw new BlueNotFoundException($"Mailing '{mailingId}' does not target cluster '{clusterId}'.");

        _db.MailingTargetClusters.Remove(target);
        await _db.SaveChangesAsync();

        // EF relationship fixup may have added `target` into an already-tracked parent Mailing's
        // TargetClusters collection; soft-delete + detach doesn't retroactively prune that, so
        // remove it explicitly to avoid a stale entry surviving within this DbContext scope.
        target.Mailing?.TargetClusters.Remove(target);
    }

    public async Task<MailingDto> AddTargetGroupInstanceAsync(Guid mailingId, Guid groupInstanceId)
    {
        var mailing = await Find(mailingId);

        var exists = await _db.MailingTargetGroupInstances.AnyAsync(x => x.MailingId == mailingId && x.UserGroupInstanceId == groupInstanceId);
        if (!exists)
        {
            _db.MailingTargetGroupInstances.Add(new MailingTargetGroupInstance { MailingId = mailingId, UserGroupInstanceId = groupInstanceId });
            await _db.SaveChangesAsync();
        }

        return ToDto(await FindQueryWithTargets().AsNoTracking().FirstAsync(x => x.Id == mailingId));
    }

    public async Task RemoveTargetGroupInstanceAsync(Guid mailingId, Guid groupInstanceId)
    {
        var target = await _db.MailingTargetGroupInstances
            .FirstOrDefaultAsync(x => x.MailingId == mailingId && x.UserGroupInstanceId == groupInstanceId)
            ?? throw new BlueNotFoundException($"Mailing '{mailingId}' does not target group instance '{groupInstanceId}'.");

        _db.MailingTargetGroupInstances.Remove(target);
        await _db.SaveChangesAsync();

        target.Mailing?.TargetGroupInstances.Remove(target);
    }

    public async Task<MailingPreviewDto> PreviewAsync(Guid mailingId)
    {
        var mailing = await Find(mailingId);
        var layout = mailing.LayoutId is null ? null : await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == mailing.LayoutId);

        var context = new MergeTokenContext(SampleTokenValues);
        var rendered = _renderer.Render(mailing.Subject, mailing.BodyMarkdown, layout?.HeaderHtml, layout?.FooterHtml, context);

        return new MailingPreviewDto(rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody);
    }

    public async Task SendProofAsync(Guid mailingId)
    {
        var mailing = await Find(mailingId);
        var layout = mailing.LayoutId is null ? null : await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == mailing.LayoutId);

        var currentUserId = _currentUserAccessor.UserId
            ?? throw new InvalidOperationException("A proof send requires an authenticated user.");
        var currentUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId)
            ?? throw new InvalidOperationException("Current user could not be resolved.");

        var context = new MergeTokenContext(SampleTokenValues);
        var rendered = _renderer.Render(mailing.Subject, mailing.BodyMarkdown, layout?.HeaderHtml, layout?.FooterHtml, context);

        _backgroundJobClient.Enqueue<MailProofSendJob>(job => job.ExecuteAsync(
            mailing.SenderKey,
            currentUser.Email!,
            rendered.Subject,
            rendered.HtmlBody,
            rendered.PlainTextBody));

        mailing.ProofSendCount++;
        await _db.SaveChangesAsync();
    }

    public async Task SendAsync(Guid mailingId)
    {
        var mailing = await FindWithTargets(mailingId);

        if (string.IsNullOrWhiteSpace(_mailOptions.PublicBaseUrl))
            throw new InvalidOperationException("Mail:PublicBaseUrl must be configured before sending a tracked mailing.");

        var layout = mailing.LayoutId is null ? null : await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == mailing.LayoutId);
        var resolved = await _targetResolver.ResolveRecipientsAsync(mailingId);

        var existingEmails = await _db.MailingRecipients
            .Where(x => x.MailingId == mailingId)
            .Select(x => x.Email)
            .ToListAsync();
        var existingEmailSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);

        foreach (var member in resolved)
        {
            if (string.IsNullOrWhiteSpace(member.Email) || existingEmailSet.Contains(member.Email))
                continue;

            _db.MailingRecipients.Add(new MailingRecipient
            {
                Id = Guid.NewGuid(),
                MailingId = mailingId,
                UserId = member.UserId,
                Email = member.Email,
                FullName = member.Fullname,
                Sent = false,
                TrackingToken = Guid.NewGuid().ToString("N"),
            });
            existingEmailSet.Add(member.Email);
        }

        await _db.SaveChangesAsync();

        var unsentRecipients = await _db.MailingRecipients
            .Where(x => x.MailingId == mailingId && !x.Sent)
            .ToListAsync();

        foreach (var recipient in unsentRecipients)
        {
            var context = recipient.UserId is not null
                ? BlueUserMergeTokenFactory.ForUser((await _db.Users.FirstOrDefaultAsync(x => x.Id == recipient.UserId))!)
                : new MergeTokenContext(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FullName"] = recipient.FullName,
                    ["Email"] = recipient.Email,
                });

            var rendered = _renderer.Render(mailing.Subject, mailing.BodyMarkdown, layout?.HeaderHtml, layout?.FooterHtml, context);
            var (htmlWithTracking, links) = RewriteLinksAndAppendPixel(rendered.HtmlBody, recipient, _mailOptions.PublicBaseUrl!);
            _db.MailingRecipientLinks.AddRange(links);

            recipient.RenderedSubject = rendered.Subject;
            recipient.RenderedHtmlBody = htmlWithTracking;
            recipient.RenderedPlainTextBody = rendered.PlainTextBody;
        }

        mailing.Status = MailingStatus.Sent;
        mailing.SentAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        foreach (var targetCluster in mailing.TargetClusters)
            targetCluster.LastSentAt = now;
        foreach (var targetInstance in mailing.TargetGroupInstances)
            targetInstance.LastSentAt = now;

        // Persist rendered content before enqueueing: Hangfire can pick up and execute a job
        // before this transaction commits, and MailingRecipientSendJob reads the recipient
        // fresh from the DB — enqueueing first was racing against SaveChangesAsync and could
        // send an empty body.
        await _db.SaveChangesAsync();

        foreach (var recipient in unsentRecipients)
        {
            _backgroundJobClient.Enqueue<MailingRecipientSendJob>(job => job.ExecuteAsync(recipient.Id));
        }
    }

    public async Task<int> GetResolvedTargetCountAsync(Guid mailingId)
    {
        await Find(mailingId);

        var resolved = await _targetResolver.ResolveRecipientsAsync(mailingId);
        return resolved.Count;
    }

    public async Task<MailingStatsDto> GetStatsAsync(Guid mailingId)
    {
        await Find(mailingId);

        var sentCount = await _db.MailingRecipients.CountAsync(x => x.MailingId == mailingId && x.Sent);
        var openedCount = await _db.MailingRecipients.CountAsync(x => x.MailingId == mailingId && x.Opened);

        var linkStats = await _db.MailingRecipientLinks
            .Where(x => x.MailingRecipient.MailingId == mailingId)
            .GroupBy(x => x.OriginalUrl)
            .Select(g => new MailingLinkStatDto(g.Key, g.Sum(x => x.ClickCount)))
            .ToListAsync();

        return new MailingStatsDto(sentCount, openedCount, linkStats);
    }

    public async Task<PagedResult<MailingRecipientDto>> GetRecipientsAsync(Guid mailingId, int page, int pageSize)
    {
        await Find(mailingId);

        var query = _db.MailingRecipients.Where(x => x.MailingId == mailingId).OrderBy(x => x.Email);
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MailingRecipientDto(x.Id, x.UserId, x.Email, x.FullName, x.Sent, x.SentAt, x.Opened, x.FirstOpenedAt, x.OpenCount))
            .ToListAsync();

        return new PagedResult<MailingRecipientDto>(items, page, pageSize, totalCount);
    }

    private static (string HtmlBody, List<MailingRecipientLink> Links) RewriteLinksAndAppendPixel(string htmlBody, MailingRecipient recipient, string publicBaseUrl)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(htmlBody);

        var links = new List<MailingRecipientLink>();
        var tokensByUrl = new Dictionary<string, string>();
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
                continue;

            if (!tokensByUrl.TryGetValue(href, out var token))
            {
                token = Guid.NewGuid().ToString("N");
                tokensByUrl[href] = token;
                links.Add(new MailingRecipientLink
                {
                    Id = Guid.NewGuid(),
                    MailingRecipientId = recipient.Id,
                    OriginalUrl = href,
                    Token = token,
                });
            }

            anchor.SetAttribute("href", $"{publicBaseUrl.TrimEnd('/')}/api/mail/r/{token}");
        }

        var pixel = document.CreateElement("img");
        pixel.SetAttribute("src", $"{publicBaseUrl.TrimEnd('/')}/api/mail/p/{recipient.TrackingToken}.gif");
        pixel.SetAttribute("width", "1");
        pixel.SetAttribute("height", "1");
        pixel.SetAttribute("style", "display:none");
        document.Body?.AppendChild(pixel);

        return (document.Body?.InnerHtml ?? htmlBody, links);
    }

    private void EnsureDraft(Mailing mailing)
    {
        if (mailing.Status != MailingStatus.Draft)
            throw new BlueValidationException($"Mailing '{mailing.Id}' can no longer be edited once it has been sent.");
    }

    private IQueryable<Mailing> FindQueryWithTargets() =>
        _db.Mailings
            .Include(x => x.TargetClusters).ThenInclude(x => x.MemberCluster)
            .Include(x => x.TargetGroupInstances).ThenInclude(x => x.UserGroupInstance).ThenInclude(x => x.UserGroup);

    private async Task<Mailing> Find(Guid id)
    {
        return await _db.Mailings.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Mailing '{id}' was not found.");
    }

    private async Task<Mailing> FindWithTargets(Guid id)
    {
        return await FindQueryWithTargets().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"Mailing '{id}' was not found.");
    }

    private static MailingDto ToDto(Mailing mailing) => new(
        mailing.Id,
        mailing.Subject,
        mailing.BodyMarkdown,
        mailing.SenderKey,
        mailing.TemplateId,
        mailing.LayoutId,
        mailing.Status,
        mailing.ProofSendCount,
        mailing.SentAt,
        mailing.TargetClusters.Select(x => new MailingTargetClusterDto(x.MemberClusterId, x.MemberCluster.Name, x.LastSentAt)).ToList(),
        mailing.TargetGroupInstances.Select(x => new MailingTargetGroupInstanceDto(x.UserGroupInstanceId, x.UserGroupInstance.UserGroup.Name, x.LastSentAt)).ToList());
}
