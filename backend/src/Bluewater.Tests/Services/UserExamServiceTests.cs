using Bluewater.Core.Dto.Exams;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Exams;
using Bluewater.Tests.TestSupport;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class UserExamServiceTests : SqliteServiceTestBase
{
    private readonly IUserExamService _sut;

    public UserExamServiceTests()
    {
        _sut = GetService<IUserExamService>();
    }

    [Fact]
    public async Task ListByUserAsync_ReturnsEmpty_WhenUserHasNoExams()
    {
        var user = await CreateUserAsync();

        var result = await _sut.ListByUserAsync(user.Id);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListByUserAsync_ReturnsOnlyExamsForThatUser()
    {
        var user = await CreateUserAsync();
        var otherUser = await CreateUserAsync("other", "other@example.com");
        var examType = await AddExamTypeAsync("VHF");

        await AddUserExamAsync(user.Id, examType.Id, new DateOnly(2025, 6, 1));
        await AddUserExamAsync(otherUser.Id, examType.Id, new DateOnly(2025, 7, 1));

        var result = await _sut.ListByUserAsync(user.Id);

        result.Count.ShouldBe(1);
        result[0].UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task ListByExamTypeAsync_ReturnsAllUsersWithThatExam()
    {
        var user1 = await CreateUserAsync("user1", "user1@example.com");
        var user2 = await CreateUserAsync("user2", "user2@example.com");
        var examType = await AddExamTypeAsync("VHF");

        await AddUserExamAsync(user1.Id, examType.Id, new DateOnly(2025, 6, 1));
        await AddUserExamAsync(user2.Id, examType.Id, new DateOnly(2025, 7, 1));

        var result = await _sut.ListByExamTypeAsync(examType.Id);

        result.Count.ShouldBe(2);
        result.Select(x => x.UserId).ShouldContain(user1.Id);
        result.Select(x => x.UserId).ShouldContain(user2.Id);
    }

    [Fact]
    public async Task AssignAsync_PersistsUserExam()
    {
        var user = await CreateUserAsync();
        var examType = await AddExamTypeAsync("VHF");
        var request = new AssignExamRequest(user.Id, examType.Id, new DateOnly(2025, 6, 1));

        var result = await _sut.AssignAsync(request);

        result.UserId.ShouldBe(user.Id);
        result.ExamTypeId.ShouldBe(examType.Id);
        result.ObtainedAt.ShouldBe(request.ObtainedAt);
        (await Db.UserExams.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task AssignAsync_Throws_WhenUserDoesNotExist()
    {
        var examType = await AddExamTypeAsync("VHF");
        var request = new AssignExamRequest(Guid.NewGuid(), examType.Id, new DateOnly(2025, 6, 1));

        await Should.ThrowAsync<ValidationException>(() => _sut.AssignAsync(request));
    }

    [Fact]
    public async Task AssignAsync_Throws_WhenExamTypeDoesNotExist()
    {
        var user = await CreateUserAsync();
        var request = new AssignExamRequest(user.Id, Guid.NewGuid(), new DateOnly(2025, 6, 1));

        await Should.ThrowAsync<ValidationException>(() => _sut.AssignAsync(request));
    }

    [Fact]
    public async Task AssignAsync_Throws_WhenUserAlreadyHasExam()
    {
        var user = await CreateUserAsync();
        var examType = await AddExamTypeAsync("VHF");
        await AddUserExamAsync(user.Id, examType.Id, new DateOnly(2025, 6, 1));

        var request = new AssignExamRequest(user.Id, examType.Id, new DateOnly(2025, 8, 1));

        await Should.ThrowAsync<ValidationException>(() => _sut.AssignAsync(request));
    }

    [Fact]
    public async Task UnassignAsync_SoftDeletesUserExam()
    {
        var user = await CreateUserAsync();
        var examType = await AddExamTypeAsync("VHF");
        var userExam = await AddUserExamAsync(user.Id, examType.Id, new DateOnly(2025, 6, 1));

        await _sut.UnassignAsync(userExam.Id);

        (await Db.UserExams.AnyAsync(x => x.Id == userExam.Id)).ShouldBeFalse();
        (await Db.UserExams.IgnoreQueryFilters().SingleAsync(x => x.Id == userExam.Id)).DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task UnassignAsync_Throws_WhenUserExamDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.UnassignAsync(Guid.NewGuid()));
    }

    private async Task<ExamType> AddExamTypeAsync(string name)
    {
        var examType = new ExamType { Id = Guid.NewGuid(), Name = name, Description = "Description" };
        Db.ExamTypes.Add(examType);
        await Db.SaveChangesAsync();
        return examType;
    }

    private async Task<UserExam> AddUserExamAsync(Guid userId, Guid examTypeId, DateOnly obtainedAt)
    {
        var userExam = new UserExam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExamTypeId = examTypeId,
            ObtainedAt = obtainedAt
        };
        Db.UserExams.Add(userExam);
        await Db.SaveChangesAsync();
        return userExam;
    }
}
