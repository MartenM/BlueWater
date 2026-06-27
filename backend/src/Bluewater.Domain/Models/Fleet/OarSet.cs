using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Fleet;

public class OarSet : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public bool Scull { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
