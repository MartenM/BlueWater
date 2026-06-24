namespace Bluewater.Core.Dto.Agenda;

public record AgendaItemDto(
    Guid Id,
    DateOnly Date,
    TimeOnly? Time,
    string Title,
    string Description,
    DateOnly? EndDate,
    TimeOnly? EndTime,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId);
