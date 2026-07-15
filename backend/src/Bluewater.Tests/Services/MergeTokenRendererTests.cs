using Bluewater.Core.Services.Mail;

namespace Bluewater.Tests.Services;

public class MergeTokenRendererTests
{
    private readonly MergeTokenRenderer _sut = new();

    [Fact]
    public void Render_SubstitutesKnownToken()
    {
        var context = new MergeTokenContext(new Dictionary<string, string> { ["FirstName"] = "Jane" });

        var result = _sut.Render("Hello {{FirstName}}!", context);

        result.ShouldBe("Hello Jane!");
    }

    [Fact]
    public void Render_LeavesUnknownTokenLiteral()
    {
        var context = new MergeTokenContext(new Dictionary<string, string> { ["FirstName"] = "Jane" });

        var result = _sut.Render("Hello {{Unknown}}!", context);

        result.ShouldBe("Hello {{Unknown}}!");
    }

    [Fact]
    public void Render_SubstitutesMultipleTokens()
    {
        var context = new MergeTokenContext(new Dictionary<string, string>
        {
            ["FirstName"] = "Jane",
            ["FullName"] = "Jane Doe",
        });

        var result = _sut.Render("{{FirstName}} a.k.a. {{FullName}}", context);

        result.ShouldBe("Jane a.k.a. Jane Doe");
    }
}
