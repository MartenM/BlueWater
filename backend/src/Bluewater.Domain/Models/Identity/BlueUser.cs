using Microsoft.AspNetCore.Identity;

namespace Bluewater.Domain.Models;

public class BlueUser : IdentityUser<Guid>
{
    public string Firstname { get; set; } = string.Empty;
    public string SurnamePrefix { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    
    public string Fullname => $"{Firstname} {(string.IsNullOrEmpty(SurnamePrefix) ? $"{Surname}" : $"{SurnamePrefix} {Surname}" )} {Surname}";
    
    public BlueAddress Address { get; set; } = new BlueAddress();
    
    public BlueAddress EmergencyAddress { get; set; } = new BlueAddress();
    public string EmergencyPhoneNumber { get; set; } = string.Empty;
    
    public DateOnly DateOfBirth { get; set; }
    
    public BlueUserSex Gender { get; set; }
}