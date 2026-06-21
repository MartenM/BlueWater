namespace Bluewater.Core.Dto.News;

public record NewsPostDto(
    Guid Id,
    string Title,
    string ShortText,
    string? AdditionalText,
    bool MembersOnly,
    Guid? IconId,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId);
