namespace Bluewater.Domain.Models;

public class BlueSeason
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    public string Name =>
        IsFullSeason()
            ? $"{StartDate.Year}/{EndDate.Year}"
            : $"{StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}";
    private bool IsFullSeason()
        => EndDate == StartDate.AddYears(1).AddDays(-1);
}