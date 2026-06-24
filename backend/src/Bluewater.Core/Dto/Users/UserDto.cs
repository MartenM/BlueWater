using Bluewater.Domain.Models;

namespace Bluewater.Core.Dto.Users;

public record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string Firstname,
    string SurnamePrefix,
    string Surname,
    string Fullname,
    string? PhoneNumber,
    BlueAddressDto Address,
    BlueAddressDto EmergencyAddress,
    string EmergencyPhoneNumber,
    DateOnly DateOfBirth,
    BlueUserSex Gender,
    Guid? ProfilePictureFileId);
