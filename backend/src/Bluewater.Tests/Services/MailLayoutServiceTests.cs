using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using FluentValidation;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MailLayoutServiceTests : SqliteServiceTestBase
{
    private readonly IMailLayoutService _sut;

    public MailLayoutServiceTests()
    {
        _sut = GetService<IMailLayoutService>();
    }

    [Fact]
    public async Task CreateAsync_ReturnsLayout()
    {
        var result = await _sut.CreateAsync(new UpsertMailLayoutRequest("Default", "<header>", "<footer>", true));

        result.Name.ShouldBe("Default");
        result.HeaderHtml.ShouldBe("<header>");
        result.FooterHtml.ShouldBe("<footer>");
        result.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var layout = await _sut.CreateAsync(new UpsertMailLayoutRequest("Old", "h", "f", false));

        var result = await _sut.UpdateAsync(layout.Id, new UpsertMailLayoutRequest("New", "h2", "f2", true));

        result.Name.ShouldBe("New");
        result.HeaderHtml.ShouldBe("h2");
        result.FooterHtml.ShouldBe("f2");
        result.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_Throws_WhenNotFound()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesLayout()
    {
        var layout = await _sut.CreateAsync(new UpsertMailLayoutRequest("Temp", "h", "f", false));

        await _sut.DeleteAsync(layout.Id);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(layout.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameEmpty()
    {
        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(new UpsertMailLayoutRequest("", "h", "f", false)));
    }
}
