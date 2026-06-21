namespace Bluewater.Core.Dto.News;

public record NewsIconDto(
    Guid Id,
    string Name,
    Guid FileId,
    DateTime CreatedAt,
    Guid CreatedByUserId);
