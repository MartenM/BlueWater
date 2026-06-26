using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Exams;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamTypeService _examTypeService;
    private readonly IUserExamService _userExamService;

    public ExamsController(IExamTypeService examTypeService, IUserExamService userExamService)
    {
        _examTypeService = examTypeService;
        _userExamService = userExamService;
    }

    /// <summary>Lists all exam types.</summary>
    [BlueAuthorize(BluePermission.ExamsView)]
    [HttpGet("types")]
    public Task<List<ExamTypeDto>> ListTypes()
    {
        return _examTypeService.ListAsync();
    }

    /// <summary>Gets a single exam type by id.</summary>
    [BlueAuthorize(BluePermission.ExamsView)]
    [HttpGet("types/{id:guid}")]
    public Task<ExamTypeDto> GetType(Guid id)
    {
        return _examTypeService.GetAsync(id);
    }

    /// <summary>Creates a new exam type.</summary>
    [BlueAuthorize(BluePermission.ExamsModify)]
    [HttpPost("types")]
    public Task<ExamTypeDto> CreateType(UpsertExamTypeRequest request)
    {
        return _examTypeService.CreateAsync(request);
    }

    /// <summary>Updates an exam type's name and description.</summary>
    [BlueAuthorize(BluePermission.ExamsModify)]
    [HttpPut("types/{id:guid}")]
    public Task<ExamTypeDto> UpdateType(Guid id, UpsertExamTypeRequest request)
    {
        return _examTypeService.UpdateAsync(id, request);
    }

    /// <summary>Deletes an exam type.</summary>
    [BlueAuthorize(BluePermission.ExamsModify)]
    [HttpDelete("types/{id:guid}")]
    public Task DeleteType(Guid id)
    {
        return _examTypeService.DeleteAsync(id);
    }

    /// <summary>Lists all exams obtained by a specific user.</summary>
    [Authorize]
    [HttpGet("by-user/{userId:guid}")]
    public Task<List<UserExamDto>> ListByUser(Guid userId)
    {
        return _userExamService.ListByUserAsync(userId);
    }

    /// <summary>Lists all users who obtained a specific exam type.</summary>
    [BlueAuthorize(BluePermission.ExamsView)]
    [HttpGet("types/{examTypeId:guid}/assigned")]
    public Task<List<UserExamDto>> ListAssigned(Guid examTypeId)
    {
        return _userExamService.ListByExamTypeAsync(examTypeId);
    }

    /// <summary>Assigns an exam to a user.</summary>
    [BlueAuthorize(BluePermission.ExamsAssign)]
    [HttpPost("assign")]
    public Task<UserExamDto> Assign(AssignExamRequest request)
    {
        return _userExamService.AssignAsync(request);
    }

    /// <summary>Removes an assigned exam from a user.</summary>
    [BlueAuthorize(BluePermission.ExamsAssign)]
    [HttpDelete("assign/{id:guid}")]
    public Task Unassign(Guid id)
    {
        return _userExamService.UnassignAsync(id);
    }
}
