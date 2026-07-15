using Bluewater.Core.Dto.Mail;
using Bluewater.Domain.Models.Mail;

namespace Bluewater.Core.Services.Abstractions;

public interface IMailTemplateService
{
    Task<List<MailTemplateDto>> ListAsync(MailTemplateKind? kind = null);
    Task<MailTemplateDto> GetAsync(Guid id);
    Task<MailTemplateDto> CreateAsync(UpsertMailTemplateRequest request);
    Task<MailTemplateDto> UpdateAsync(Guid id, UpsertMailTemplateRequest request);
    Task DeleteAsync(Guid id);
    Task<MailTemplatePreviewDto> PreviewAsync(Guid id, MailTemplatePreviewRequest request);
    Task<List<MailPlaceholderDto>> GetPlaceholdersAsync(Guid? templateId);
}
