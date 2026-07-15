using Bluewater.Domain.Models.Mail;

namespace Bluewater.Core.Dto.Mail;

public record MailingDto(
    Guid Id,
    string Subject,
    string BodyMarkdown,
    string SenderKey,
    Guid? TemplateId,
    Guid? LayoutId,
    MailingStatus Status,
    int ProofSendCount,
    DateTime? SentAt,
    List<MailingTargetClusterDto> TargetClusters,
    List<MailingTargetGroupInstanceDto> TargetGroupInstances);

public record MailingTargetClusterDto(Guid MemberClusterId, string MemberClusterName, DateTime? LastSentAt);

public record MailingTargetGroupInstanceDto(Guid UserGroupInstanceId, string UserGroupName, DateTime? LastSentAt);
