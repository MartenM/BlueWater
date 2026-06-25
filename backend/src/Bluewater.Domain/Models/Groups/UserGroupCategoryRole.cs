using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Groups;

public class UserGroupCategoryRole : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserGroupCategoryId { get; set; }
    public UserGroupCategory UserGroupCategory { get; set; } = null!;

    public int SortOrder { get; set; }

    public string NamePlural { get; set; } = string.Empty;
    public string? NameMasculine { get; set; }
    public string? NameFeminine { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
