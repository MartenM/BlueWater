using Bluewater.Domain.Models.Outings;

namespace Bluewater.Core.Dto.Outings;

public record OutingChangelogEntryDto(
    Guid Id,
    OutingChangelogType Type,
    string Fields,
    DateTime CreatedAt,
    Guid ActorUserId,
    string ActorFullname);
