using Bluewater.Core.Dto.Exams;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Exams;
using Bluewater.Tests.TestSupport;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class ExamTypeServiceTests : SqliteServiceTestBase
{
    private readonly IExamTypeService _sut;

    public ExamTypeServiceTests()
    {
        _sut = GetService<IExamTypeService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoExamTypesExist()
    {
        var result = await _sut.ListAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllExamTypes_OrderedByName()
    {
        await AddExamTypeAsync("Zeilbewijs");
        await AddExamTypeAsync("Ankerkunde");

        var result = await _sut.ListAsync();

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Ankerkunde");
        result[1].Name.ShouldBe("Zeilbewijs");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenExamTypeDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsExamType_AndStampsAuditFields()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var request = new UpsertExamTypeRequest("VHF Marifoon", "Radiotelefonie certificaat");

        var result = await _sut.CreateAsync(request);

        result.Name.ShouldBe(request.Name);
        result.Description.ShouldBe(request.Description);
        (await Db.ExamTypes.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
        (await Db.ExamTypes.SingleAsync(x => x.Id == result.Id)).CreatedByUserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameIsEmpty()
    {
        var request = new UpsertExamTypeRequest("", "Some description");

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameIsTooShort()
    {
        var request = new UpsertExamTypeRequest("X", "Some description");

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingExamType()
    {
        var examType = await AddExamTypeAsync("Old name");

        var result = await _sut.UpdateAsync(examType.Id, new UpsertExamTypeRequest("New name", "New description"));

        result.Name.ShouldBe("New name");
        result.Description.ShouldBe("New description");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenExamTypeDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertExamTypeRequest("Name", "Description")));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesExamType()
    {
        var examType = await AddExamTypeAsync("To delete");

        await _sut.DeleteAsync(examType.Id);

        (await Db.ExamTypes.AnyAsync(x => x.Id == examType.Id)).ShouldBeFalse();
        (await Db.ExamTypes.IgnoreQueryFilters().SingleAsync(x => x.Id == examType.Id)).DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenExamTypeDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private async Task<ExamType> AddExamTypeAsync(string name, string description = "Description")
    {
        var examType = new ExamType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
        Db.ExamTypes.Add(examType);
        await Db.SaveChangesAsync();
        return examType;
    }
}
