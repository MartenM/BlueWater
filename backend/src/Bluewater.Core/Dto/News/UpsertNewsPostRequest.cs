namespace Bluewater.Core.Dto.News;

public record UpsertNewsPostRequest(
    string Title,
    string ShortText,
    string? AdditionalText,
    bool MembersOnly);
