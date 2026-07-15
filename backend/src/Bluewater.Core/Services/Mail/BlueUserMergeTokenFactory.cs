using Bluewater.Domain.Models;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// The one place that knows how to turn a <see cref="BlueUser"/> into the standard merge-token
/// set. Every real-BlueUser recipient in both the transactional and bulk mailing paths goes
/// through this factory.
/// </summary>
public static class BlueUserMergeTokenFactory
{
    public static MergeTokenContext ForUser(BlueUser user)
    {
        var addressBlock = string.Join(", ",
            new[] { user.Address.Address, user.Address.Zip, user.Address.City }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = user.Firstname,
            ["FullName"] = user.Fullname,
            ["Email"] = user.Email ?? string.Empty,
            ["FormalSalutation"] = $"Dear {user.Fullname}",
            ["AddressBlock"] = addressBlock,
        };

        return new MergeTokenContext(values);
    }
}
