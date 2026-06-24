using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Agenda;

public class AgendaItem : IAuditable
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public TimeOnly? EndTime { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
