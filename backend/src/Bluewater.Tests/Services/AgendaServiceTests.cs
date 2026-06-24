using Bluewater.Core.Dto.Agenda;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Agenda;
using Bluewater.Tests.TestSupport;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class AgendaServiceTests : SqliteServiceTestBase
{
    private readonly IAgendaService _sut;

    public AgendaServiceTests()
    {
        _sut = GetService<IAgendaService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoItemsExist()
    {
        var result = await _sut.ListAsync(1, 20);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_OrdersSoonestFirst_AndPaginates()
    {
        var later = await AddItemAsync("Later", date: new DateOnly(2026, 7, 3));
        var soonest = await AddItemAsync("Soonest", date: new DateOnly(2026, 7, 1));
        var middle = await AddItemAsync("Middle", date: new DateOnly(2026, 7, 2));

        var page1 = await _sut.ListAsync(1, 2);
        page1.Items.Select(x => x.Id).ShouldBe([soonest.Id, middle.Id]);
        page1.TotalCount.ShouldBe(3);

        var page2 = await _sut.ListAsync(2, 2);
        page2.Items.Select(x => x.Id).ShouldBe([later.Id]);
    }

    [Fact]
    public async Task ListRangeAsync_IncludesItem_FullyInsideRange()
    {
        var item = await AddItemAsync("Inside", date: new DateOnly(2026, 7, 10));

        var result = await _sut.ListRangeAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        result.Select(x => x.Id).ShouldContain(item.Id);
    }

    [Fact]
    public async Task ListRangeAsync_IncludesItem_OverlappingRangeBoundary()
    {
        var item = await AddItemAsync("Spanning", date: new DateOnly(2026, 6, 28), endDate: new DateOnly(2026, 7, 3));

        var result = await _sut.ListRangeAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        result.Select(x => x.Id).ShouldContain(item.Id);
    }

    [Fact]
    public async Task ListRangeAsync_ExcludesItem_EntirelyBeforeRange()
    {
        await AddItemAsync("Before", date: new DateOnly(2026, 6, 1));

        var result = await _sut.ListRangeAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListRangeAsync_ExcludesItem_EntirelyAfterRange()
    {
        await AddItemAsync("After", date: new DateOnly(2026, 8, 1));

        var result = await _sut.ListRangeAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListRangeAsync_IncludesSingleDayItem_AtRangeEdge()
    {
        var item = await AddItemAsync("Edge", date: new DateOnly(2026, 7, 31));

        var result = await _sut.ListRangeAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        result.Select(x => x.Id).ShouldContain(item.Id);
    }

    [Fact]
    public async Task ListUpcomingAsync_ExcludesPastItem()
    {
        await AddItemAsync("Past", date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));

        var result = await _sut.ListUpcomingAsync(5);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListUpcomingAsync_IncludesFutureItems_InDateOrder_AndRespectsCount()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = await AddItemAsync("First", date: today.AddDays(1));
        var second = await AddItemAsync("Second", date: today.AddDays(2));
        await AddItemAsync("Third", date: today.AddDays(3));

        var result = await _sut.ListUpcomingAsync(2);

        result.Select(x => x.Id).ShouldBe([first.Id, second.Id]);
    }

    [Fact]
    public async Task ListUpcomingAsync_IncludesInProgressMultiDayItem()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var inProgress = await AddItemAsync("InProgress", date: today.AddDays(-2), endDate: today.AddDays(2));

        var result = await _sut.ListUpcomingAsync(5);

        result.Select(x => x.Id).ShouldContain(inProgress.Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsItem_WhenItExists()
    {
        var item = await AddItemAsync("Title");

        var result = await _sut.GetAsync(item.Id);

        result.Title.ShouldBe("Title");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenItemDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsItem_AndStampsAuditFields()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var request = new UpsertAgendaItemRequest(new DateOnly(2026, 7, 1), new TimeOnly(9, 0), "Title", "Description", null, null);

        var result = await _sut.CreateAsync(request);

        result.Title.ShouldBe(request.Title);
        result.Description.ShouldBe(request.Description);
        result.Date.ShouldBe(request.Date);
        result.Time.ShouldBe(request.Time);
        result.CreatedByUserId.ShouldBe(user.Id);
        (await Db.AgendaItems.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenTitleIsEmpty()
    {
        var request = new UpsertAgendaItemRequest(new DateOnly(2026, 7, 1), null, "", "Description", null, null);

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDescriptionIsEmpty()
    {
        var request = new UpsertAgendaItemRequest(new DateOnly(2026, 7, 1), null, "Title", "", null, null);

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEndDateIsBeforeDate()
    {
        var request = new UpsertAgendaItemRequest(
            new DateOnly(2026, 7, 10), null, "Title", "Description", new DateOnly(2026, 7, 5), null);

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEndTimeIsSetWithoutTime()
    {
        var request = new UpsertAgendaItemRequest(
            new DateOnly(2026, 7, 1), null, "Title", "Description", null, new TimeOnly(10, 0));

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEndTimeIsBeforeTime_OnSameDay()
    {
        var request = new UpsertAgendaItemRequest(
            new DateOnly(2026, 7, 1), new TimeOnly(10, 0), "Title", "Description", null, new TimeOnly(9, 0));

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingItem_AndStampsAuditFields()
    {
        var item = await AddItemAsync("Original");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;

        var result = await _sut.UpdateAsync(item.Id, new UpsertAgendaItemRequest(
            new DateOnly(2026, 7, 1), new TimeOnly(9, 0), "Renamed", "New description", null, null));

        result.Title.ShouldBe("Renamed");
        result.Description.ShouldBe("New description");
        result.UpdatedByUserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenTitleIsEmpty()
    {
        var item = await AddItemAsync("Title");

        await Should.ThrowAsync<ValidationException>(
            () => _sut.UpdateAsync(item.Id, new UpsertAgendaItemRequest(new DateOnly(2026, 7, 1), null, "", "Description", null, null)));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenItemDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertAgendaItemRequest(new DateOnly(2026, 7, 1), null, "Title", "Description", null, null)));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesItem()
    {
        var item = await AddItemAsync("Title");

        await _sut.DeleteAsync(item.Id);

        (await Db.AgendaItems.AnyAsync(x => x.Id == item.Id)).ShouldBeFalse();
        (await Db.AgendaItems.IgnoreQueryFilters().SingleAsync(x => x.Id == item.Id)).DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenItemDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private async Task<AgendaItem> AddItemAsync(
        string title, DateOnly? date = null, DateOnly? endDate = null)
    {
        var item = new AgendaItem
        {
            Id = Guid.NewGuid(),
            Date = date ?? new DateOnly(2026, 7, 1),
            Title = title,
            Description = "Description",
            EndDate = endDate
        };
        Db.AgendaItems.Add(item);
        await Db.SaveChangesAsync();

        return item;
    }
}
