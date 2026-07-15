namespace Bluewater.Core.Services.Mail;

public interface IMergeTokenRenderer
{
    /// <summary>
    /// Substitutes <c>{{Token}}</c> placeholders using values from <paramref name="context"/>.
    /// Unknown tokens are left literally in place (not blanked) so bugs are visible.
    /// </summary>
    string Render(string source, MergeTokenContext context);
}
