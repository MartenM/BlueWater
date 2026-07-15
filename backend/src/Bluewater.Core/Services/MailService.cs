using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Services.Mail;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MailService : IMailService
{
    private readonly BluewaterContext _db;
    private readonly IValidator<SendTransactionalMailRequest> _validator;
    private readonly IMailContentRenderer _renderer;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public MailService(
        BluewaterContext db,
        IValidator<SendTransactionalMailRequest> validator,
        IMailContentRenderer renderer,
        IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _validator = validator;
        _renderer = renderer;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task SendTransactionalAsync(SendTransactionalMailRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        MailTemplate? template = null;
        MailLayout? layout = null;
        if (request.TemplateId is not null)
        {
            template = await _db.MailTemplates.FirstOrDefaultAsync(x => x.Id == request.TemplateId)
                ?? throw new BlueNotFoundException($"MailTemplate '{request.TemplateId}' was not found.");

            if (template.DefaultLayoutId is not null)
            {
                layout = await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == template.DefaultLayoutId);
            }
        }

        var subjectTemplate = request.SubjectOverride ?? template?.SubjectTemplate ?? string.Empty;
        var bodyMarkdown = request.BodyMarkdownOverride ?? template?.BodyMarkdown ?? string.Empty;
        var senderKey = request.SenderKey;

        foreach (var recipient in request.Recipients)
        {
            var context = MergeTokenContext.Empty;
            if (recipient.UserId is not null)
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == recipient.UserId);
                if (user is not null)
                {
                    context = BlueUserMergeTokenFactory.ForUser(user);
                }
            }
            context = context.With(recipient.MergeValues);

            var rendered = _renderer.Render(subjectTemplate, bodyMarkdown, layout?.HeaderHtml, layout?.FooterHtml, context);

            _backgroundJobClient.Enqueue<TransactionalMailJob>(job => job.ExecuteAsync(
                senderKey,
                new List<string> { recipient.Email },
                request.Cc,
                request.Bcc,
                request.ReplyToOverride,
                rendered.Subject,
                rendered.HtmlBody,
                rendered.PlainTextBody,
                request.AttachmentStoredFileIds));
        }
    }
}
