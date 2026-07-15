namespace Bluewater.Core.Dto.Mail;

public record MailingStatsDto(
    int SentCount,
    int OpenedCount,
    List<MailingLinkStatDto> LinkStats);

public record MailingLinkStatDto(string OriginalUrl, int ClickCount);
