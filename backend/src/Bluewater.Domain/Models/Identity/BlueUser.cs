using Bluewater.Domain.Models.Files;
using Microsoft.AspNetCore.Identity;

namespace Bluewater.Domain.Models;

public class BlueUser : IdentityUser<Guid>
{
    public string Firstname { get; set; } = string.Empty;
    public string SurnamePrefix { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;

    public string Fullname => string.IsNullOrEmpty(SurnamePrefix) ? $"{Firstname} {Surname}" : $"{Firstname} {SurnamePrefix} {Surname}";

    public BlueAddress Address { get; set; } = new BlueAddress();

    public Guid? ProfilePictureFileId { get; set; }
    public StoredFile? ProfilePicture { get; set; }
    
    public BlueAddress EmergencyAddress { get; set; } = new BlueAddress();
    public string EmergencyPhoneNumber { get; set; } = string.Empty;
    
    public DateOnly DateOfBirth { get; set; }
    
    public BlueUserSex Gender { get; set; }
}