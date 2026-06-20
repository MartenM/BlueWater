using Microsoft.AspNetCore.Identity;

namespace Bluewater.Domain.Models;

public class BlueUser : IdentityUser<Guid>
{
    public string Firstname { get; set; } = string.Empty;
    public string SurnamePrefix { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    
    public string Fullname => $"{Firstname} {(string.IsNullOrEmpty(SurnamePrefix) ? $"{Surname}" : $"{SurnamePrefix} {Surname}" )} {Surname}";
}