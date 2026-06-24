using Bluewater.Domain.Models;

namespace Bluewater.Core.Dto.Users;

public record CreateUserRequest(
    string UserName,
    string Email,
    string Firstname,
    string SurnamePrefix,
    string Surname,
    string? PhoneNumber,
    BlueAddressDto Address,
    BlueAddressDto EmergencyAddress,
    string EmergencyPhoneNumber,
    DateOnly DateOfBirth,
    BlueUserSex Gender);
