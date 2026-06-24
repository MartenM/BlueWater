namespace Bluewater.Core.Dto.Users;

public record CreateUserResponse(UserDto User, string GeneratedPassword);
