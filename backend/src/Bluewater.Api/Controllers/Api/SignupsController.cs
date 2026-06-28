using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/signups")]
public class SignupsController : ControllerBase
{
    private readonly ISignupService _service;

    public SignupsController(ISignupService service)
    {
        _service = service;
    }

    [BlueAuthorize(BluePermission.SignupView)]
    [HttpGet]
    public Task<List<SignupListItemDto>> List() => _service.ListForMemberAsync();

    [BlueAuthorize(BluePermission.SignupRespond)]
    [HttpGet("my")]
    public Task<List<SignupListItemDto>> My() => _service.GetMyResponsesAsync();

    [BlueAuthorize(BluePermission.SignupView)]
    [HttpGet("{id:guid}")]
    public Task<SignupDetailDto> Get(Guid id) => _service.GetForMemberAsync(id);

    [BlueAuthorize(BluePermission.SignupRespond)]
    [HttpPost("{id:guid}/responses")]
    public Task<SignupResponseDto> Submit(Guid id, SubmitResponseRequest request) => _service.SubmitResponseAsync(id, request);

    [BlueAuthorize(BluePermission.SignupRespond)]
    [HttpPut("{id:guid}/responses/{responseId:guid}")]
    public Task<SignupResponseDto> UpdateResponse(Guid id, Guid responseId, UpdateResponseRequest request)
        => _service.UpdateMyResponseAsync(id, responseId, request);

    [BlueAuthorize(BluePermission.SignupRespond)]
    [HttpDelete("{id:guid}/responses/{responseId:guid}")]
    public Task DeleteResponse(Guid id, Guid responseId) => _service.DeleteMyResponseAsync(id, responseId);
}
