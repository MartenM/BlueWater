namespace Bluewater.Domain.Models.Mail;

// Mirrors the token set BlueUserMergeTokenFactory (Bluewater.Core) produces for every
// real-BlueUser recipient. Kept here in Domain rather than Core so both Core services and the
// Infra seeder can reference the same catalog without a circular project reference
// (Core -> Infra -> Domain).
public static class MailPlaceholderCatalog
{
    public static readonly IReadOnlyList<MailPlaceholderInfo> Base =
    [
        new("FirstName", "Voornaam van de ontvanger.", "Jane"),
        new("FullName", "Volledige naam van de ontvanger.", "Jane Doe"),
        new("Email", "E-mailadres van de ontvanger.", "jane.doe@example.com"),
        new("FormalSalutation", "Formele aanhef, bijv. \"Dear Jane Doe\" (vast formaat, niet vertaald).", "Dear Jane Doe"),
        new("AddressBlock", "Bekend adres van de ontvanger.", "Samplestraat 1, 1234 AB, Sample City"),
    ];
}
