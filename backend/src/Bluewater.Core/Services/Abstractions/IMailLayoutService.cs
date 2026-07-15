using Bluewater.Core.Dto.Mail;

namespace Bluewater.Core.Services.Abstractions;

public interface IMailLayoutService
{
    Task<List<MailLayoutDto>> ListAsync();
    Task<MailLayoutDto> GetAsync(Guid id);
    Task<MailLayoutDto> CreateAsync(UpsertMailLayoutRequest request);
    Task<MailLayoutDto> UpdateAsync(Guid id, UpsertMailLayoutRequest request);
    Task DeleteAsync(Guid id);
}
