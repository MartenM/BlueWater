using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Mail;

namespace Bluewater.Core.Services.Abstractions;

public interface IMailingService
{
    Task<List<MailingDto>> ListAsync();
    Task<MailingDto> GetAsync(Guid id);
    Task<MailingDto> CreateAsync(UpsertMailingRequest request);
    Task<MailingDto> UpdateAsync(Guid id, UpsertMailingRequest request);
    Task DeleteAsync(Guid id);

    Task<MailingDto> AddTargetClusterAsync(Guid mailingId, Guid clusterId);
    Task RemoveTargetClusterAsync(Guid mailingId, Guid clusterId);
    Task<MailingDto> AddTargetGroupInstanceAsync(Guid mailingId, Guid groupInstanceId);
    Task RemoveTargetGroupInstanceAsync(Guid mailingId, Guid groupInstanceId);

    Task<MailingPreviewDto> PreviewAsync(Guid mailingId);
    Task SendProofAsync(Guid mailingId);
    Task SendAsync(Guid mailingId);
    Task<MailingStatsDto> GetStatsAsync(Guid mailingId);
    Task<int> GetResolvedTargetCountAsync(Guid mailingId);
    Task<PagedResult<MailingRecipientDto>> GetRecipientsAsync(Guid mailingId, int page, int pageSize);
}
