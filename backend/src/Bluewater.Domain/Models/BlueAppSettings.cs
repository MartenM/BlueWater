namespace Bluewater.Domain.Models;

public class BlueAppSettings
{
    public const int SingletonId = 1;
    
    public int Id { get; set; } = SingletonId;
    public bool LoginEnabled { get; set; } = true;

    public BlueSeason CurrentSeason { get; set; } = null!;
    public Guid CurrentSeasonId { get; set; }
}