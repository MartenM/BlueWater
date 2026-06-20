using Microsoft.EntityFrameworkCore;

namespace Bluewater.Domain.Models;

[Owned]
public class BlueAddress
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}