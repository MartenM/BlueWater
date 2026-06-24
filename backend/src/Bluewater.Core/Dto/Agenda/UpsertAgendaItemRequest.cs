namespace Bluewater.Core.Dto.Agenda;

public record UpsertAgendaItemRequest(
    DateOnly Date,
    TimeOnly? Time,
    string Title,
    string Description,
    DateOnly? EndDate,
    TimeOnly? EndTime);
