using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto;

public record LoginRequest([Required] [EmailAddress] string Email, [Required] string Password);