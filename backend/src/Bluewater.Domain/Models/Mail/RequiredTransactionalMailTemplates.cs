namespace Bluewater.Domain.Models.Mail;

public static class TransactionalMailTemplateNames
{
    public const string WelcomeEmail = "Welcome Email";
    public const string PasswordReset = "Password Reset";
}

public record RequiredTransactionalMailTemplateDefinition(
    string Name,
    string Description,
    string SubjectTemplate,
    string BodyMarkdown,
    IReadOnlyList<MailPlaceholderInfo> ExtraPlaceholders);

// The full set of Transactional MailTemplates the application's code depends on by exact Name
// (e.g. UserService looks up TransactionalMailTemplateNames.WelcomeEmail). Seeded automatically
// by BluewaterContextSeeder on every startup so an operator never has to hand-create a template
// matching a hardcoded lookup string just to make a code path work — see MailTemplateService's
// create/rename/delete guards, which keep these names stable once seeded.
public static class RequiredTransactionalMailTemplates
{
    public static readonly RequiredTransactionalMailTemplateDefinition WelcomeEmail = new(
        TransactionalMailTemplateNames.WelcomeEmail,
        "Verzonden zodra een nieuw lid wordt aangemaakt.",
        "Welkom!",
        "Beste {{FirstName}},\n\nWelkom! Je account is aangemaakt.",
        ExtraPlaceholders: []);

    // Not yet wired to an actual send-path in code (no password-reset flow exists yet) — seeded
    // ahead of time so the name/content are ready and consistent once that flow is built, rather
    // than having someone hand-create it later under an arbitrary name.
    public static readonly RequiredTransactionalMailTemplateDefinition PasswordReset = new(
        TransactionalMailTemplateNames.PasswordReset,
        "Verzonden wanneer een lid een wachtwoordherstel aanvraagt.",
        "Wachtwoord opnieuw instellen",
        "Beste {{FirstName}},\n\nJe wachtwoord is gereset. Je nieuwe wachtwoord is: {{Password}}.",
        ExtraPlaceholders: [new MailPlaceholderInfo("Password", "The password of the user", "75uvKpHjJJwaivIV")]);

    public static readonly IReadOnlyList<RequiredTransactionalMailTemplateDefinition> All = [WelcomeEmail, PasswordReset];
}
