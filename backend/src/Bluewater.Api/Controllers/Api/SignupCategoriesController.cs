using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/signup-categories")]
public class SignupCategoriesController : ControllerBase
{
    private readonly ISignupCategoryService _service;

    public SignupCategoriesController(ISignupCategoryService service)
    {
        _service = service;
    }

    [BlueAuthorize(BluePermission.AdminSignupView, BluePermission.AdminSignupModify)]
    [HttpGet]
    public Task<List<SignupCategoryDto>> List() => _service.ListAsync();

    [BlueAuthorize(BluePermission.AdminSignupView, BluePermission.AdminSignupModify)]
    [HttpGet("{id:guid}")]
    public Task<SignupCategoryDto> Get(Guid id) => _service.GetAsync(id);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPost]
    public Task<SignupCategoryDto> Create(UpsertSignupCategoryRequest request) => _service.CreateAsync(request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPut("{id:guid}")]
    public Task<SignupCategoryDto> Update(Guid id, UpsertSignupCategoryRequest request) => _service.UpdateAsync(id, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id) => _service.DeleteAsync(id);
}
