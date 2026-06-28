namespace Bluewater.Core.Dto.Signup;

public record SignupArchiveSeasonDto(
    string SeasonName,
    List<SignupListItemDto> Signups);
