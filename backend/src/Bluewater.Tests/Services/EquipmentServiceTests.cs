using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Exams;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class EquipmentServiceTests : SqliteServiceTestBase
{
    private readonly IEquipmentService _sut;

    public EquipmentServiceTests()
    {
        _sut = GetService<IEquipmentService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoEquipmentExists()
    {
        var result = await _sut.ListAsync(1, 10, null);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenEquipmentDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsEquipment_WithoutRequiredExamType()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var request = new UpsertEquipmentRequest("Skiff", null, null, null, null, null, false, false, null, null, null, null, null);

        var result = await _sut.CreateAsync(request);

        result.Name.ShouldBe("Skiff");
        result.RequiredExamTypeId.ShouldBeNull();
        result.RequiredExamTypeName.ShouldBeNull();
        (await Db.Equipment.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_PersistsEquipment_WithRequiredExamType()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var examType = await AddExamTypeAsync("Roeibewijs");
        var request = new UpsertEquipmentRequest("Vier-zonder", null, null, null, null, examType.Id, false, false, null, null, null, null, null);

        var result = await _sut.CreateAsync(request);

        result.RequiredExamTypeId.ShouldBe(examType.Id);
        result.RequiredExamTypeName.ShouldBe("Roeibewijs");
    }

    [Fact]
    public async Task UpdateAsync_ClearsRequiredExamType_WhenSetToNull()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var examType = await AddExamTypeAsync("Roeibewijs");
        var createRequest = new UpsertEquipmentRequest("Twee-met", null, null, null, null, examType.Id, false, false, null, null, null, null, null);
        var created = await _sut.CreateAsync(createRequest);

        var updateRequest = new UpsertEquipmentRequest("Twee-met", null, null, null, null, null, false, false, null, null, null, null, null);
        var result = await _sut.UpdateAsync(created.Id, updateRequest);

        result.RequiredExamTypeId.ShouldBeNull();
        result.RequiredExamTypeName.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenEquipmentDoesNotExist()
    {
        var request = new UpsertEquipmentRequest("Name", null, null, null, null, null, false, false, null, null, null, null, null);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEquipment()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        var request = new UpsertEquipmentRequest("To delete", null, null, null, null, null, false, false, null, null, null, null, null);
        var created = await _sut.CreateAsync(request);

        await _sut.DeleteAsync(created.Id);

        (await Db.Equipment.AnyAsync(x => x.Id == created.Id)).ShouldBeFalse();
        (await Db.Equipment.IgnoreQueryFilters().SingleAsync(x => x.Id == created.Id)).DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenEquipmentDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private async Task<ExamType> AddExamTypeAsync(string name)
    {
        var examType = new ExamType { Id = Guid.NewGuid(), Name = name, Description = string.Empty };
        Db.ExamTypes.Add(examType);
        await Db.SaveChangesAsync();
        return examType;
    }
}
