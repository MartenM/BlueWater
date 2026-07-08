namespace Bluewater.Core.Dto.Availability;

public record AvailabilityMemberWeekDto(
    Guid UserId,
    string Fullname,
    bool HasProfilePicture,
    List<AvailabilityDayDto> Days);
