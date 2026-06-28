namespace Bluewater.Core.Dto.Signup;

public record SignupCategoryDto(Guid Id, string Title, bool Hidden, int SortOrder);

public record UpsertSignupCategoryRequest(string Title, bool Hidden, int SortOrder);
