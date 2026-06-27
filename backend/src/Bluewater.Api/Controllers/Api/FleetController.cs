using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Fleet;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class FleetController : ControllerBase
{
    private readonly IManufacturerService _manufacturerService;
    private readonly IFleetEquipmentTypeService _equipmentTypeService;
    private readonly IOarSetService _oarSetService;
    private readonly IEquipmentService _equipmentService;

    public FleetController(
        IManufacturerService manufacturerService,
        IFleetEquipmentTypeService equipmentTypeService,
        IOarSetService oarSetService,
        IEquipmentService equipmentService)
    {
        _manufacturerService = manufacturerService;
        _equipmentTypeService = equipmentTypeService;
        _oarSetService = oarSetService;
        _equipmentService = equipmentService;
    }

    // --- Equipment ---

    /// <summary>Lists equipment with optional search and pagination.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet]
    public Task<PagedResult<EquipmentDto>> ListEquipment([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        return _equipmentService.ListAsync(page, pageSize, search);
    }

    /// <summary>Gets a single piece of equipment by id.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("{id:guid}")]
    public Task<EquipmentDto> GetEquipment(Guid id)
    {
        return _equipmentService.GetAsync(id);
    }

    /// <summary>Creates a new piece of equipment.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPost]
    public Task<EquipmentDto> CreateEquipment(UpsertEquipmentRequest request)
    {
        return _equipmentService.CreateAsync(request);
    }

    /// <summary>Updates a piece of equipment.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPut("{id:guid}")]
    public Task<EquipmentDto> UpdateEquipment(Guid id, UpsertEquipmentRequest request)
    {
        return _equipmentService.UpdateAsync(id, request);
    }

    /// <summary>Deletes a piece of equipment.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpDelete("{id:guid}")]
    public Task DeleteEquipment(Guid id)
    {
        return _equipmentService.DeleteAsync(id);
    }

    // --- Equipment Types ---

    /// <summary>Lists all equipment types.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("types")]
    public Task<List<EquipmentTypeDto>> ListTypes()
    {
        return _equipmentTypeService.ListAsync();
    }

    /// <summary>Gets a single equipment type by id.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("types/{id:guid}")]
    public Task<EquipmentTypeDto> GetType(Guid id)
    {
        return _equipmentTypeService.GetAsync(id);
    }

    /// <summary>Creates a new equipment type.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPost("types")]
    public Task<EquipmentTypeDto> CreateType(UpsertEquipmentTypeRequest request)
    {
        return _equipmentTypeService.CreateAsync(request);
    }

    /// <summary>Updates an equipment type.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPut("types/{id:guid}")]
    public Task<EquipmentTypeDto> UpdateType(Guid id, UpsertEquipmentTypeRequest request)
    {
        return _equipmentTypeService.UpdateAsync(id, request);
    }

    /// <summary>Deletes an equipment type.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpDelete("types/{id:guid}")]
    public Task DeleteType(Guid id)
    {
        return _equipmentTypeService.DeleteAsync(id);
    }

    // --- Manufacturers ---

    /// <summary>Lists all manufacturers.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("manufacturers")]
    public Task<List<ManufacturerDto>> ListManufacturers()
    {
        return _manufacturerService.ListAsync();
    }

    /// <summary>Gets a single manufacturer by id.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("manufacturers/{id:guid}")]
    public Task<ManufacturerDto> GetManufacturer(Guid id)
    {
        return _manufacturerService.GetAsync(id);
    }

    /// <summary>Creates a new manufacturer.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPost("manufacturers")]
    public Task<ManufacturerDto> CreateManufacturer(UpsertManufacturerRequest request)
    {
        return _manufacturerService.CreateAsync(request);
    }

    /// <summary>Updates a manufacturer.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPut("manufacturers/{id:guid}")]
    public Task<ManufacturerDto> UpdateManufacturer(Guid id, UpsertManufacturerRequest request)
    {
        return _manufacturerService.UpdateAsync(id, request);
    }

    /// <summary>Deletes a manufacturer.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpDelete("manufacturers/{id:guid}")]
    public Task DeleteManufacturer(Guid id)
    {
        return _manufacturerService.DeleteAsync(id);
    }

    // --- Oar Sets ---

    /// <summary>Lists all oar sets.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("oar-sets")]
    public Task<List<OarSetDto>> ListOarSets()
    {
        return _oarSetService.ListAsync();
    }

    /// <summary>Gets a single oar set by id.</summary>
    [BlueAuthorize(BluePermission.FleetView)]
    [HttpGet("oar-sets/{id:guid}")]
    public Task<OarSetDto> GetOarSet(Guid id)
    {
        return _oarSetService.GetAsync(id);
    }

    /// <summary>Creates a new oar set.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPost("oar-sets")]
    public Task<OarSetDto> CreateOarSet(UpsertOarSetRequest request)
    {
        return _oarSetService.CreateAsync(request);
    }

    /// <summary>Updates an oar set.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpPut("oar-sets/{id:guid}")]
    public Task<OarSetDto> UpdateOarSet(Guid id, UpsertOarSetRequest request)
    {
        return _oarSetService.UpdateAsync(id, request);
    }

    /// <summary>Deletes an oar set.</summary>
    [BlueAuthorize(BluePermission.FleetModify)]
    [HttpDelete("oar-sets/{id:guid}")]
    public Task DeleteOarSet(Guid id)
    {
        return _oarSetService.DeleteAsync(id);
    }
}
