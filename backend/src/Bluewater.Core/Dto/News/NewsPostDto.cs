namespace Bluewater.Core.Dto.News;

public record NewsPostDto(
    Guid Id,
    string Title,
    string ShortText,
    string? AdditionalText,
    bool MembersOnly,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId);
