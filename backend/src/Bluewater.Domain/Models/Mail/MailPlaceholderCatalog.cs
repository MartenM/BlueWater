namespace Bluewater.Domain.Models.Mail;

// Mirrors the token set BlueUserMergeTokenFactory (Bluewater.Core) produces for every
// real-BlueUser recipient. Kept here in Domain rather than Core so both Core services and the
// Infra seeder can reference the same catalog without a circular project reference
// (Core -> Infra -> Domain).
public static class MailPlaceholderCatalog
{
    public static readonly IReadOnlyList<MailPlaceholderInfo> Base =
    [
        new("FirstName", "Voornaam van de ontvanger.", "Henk"),
        new("FullName", "Volledige naam van de ontvanger.", "Henk Tulpenaar"),
        new("Email", "E-mailadres van de ontvanger.", "henk.tulpenaar@example.com"),
        new("AddressBlock", "Bekend adres van de ontvanger.", "Ligusterlaan 4, Klein Zanikem, Surrey"),
    ];
}
