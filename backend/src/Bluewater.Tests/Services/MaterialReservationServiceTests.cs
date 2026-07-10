using Bluewater.Core.Dto.MaterialPlanner;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class MaterialReservationServiceTests : SqliteServiceTestBase
{
    private readonly IMaterialReservationService _sut;

    public MaterialReservationServiceTests()
    {
        _sut = GetService<IMaterialReservationService>();
    }

    // -------------------------------------------------------------------------
    // GetDayAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDayAsync_GroupsBoatsByType_AndFallsBackToOverigForUntyped()
    {
        var typed = await CreateBoatAsync("Skiff", typeName: "Skiff");
        var untyped = await CreateBoatAsync("Loose", equipmentType: null);

        var result = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));

        result.BoatTypeGroups.ShouldContain(g => g.TypeLabel == "Skiff" && g.Boats.Any(b => b.EquipmentId == typed.Id));
        result.BoatTypeGroups.ShouldContain(g => g.TypeLabel == "Overig" && g.Boats.Any(b => b.EquipmentId == untyped.Id));
    }

    [Fact]
    public async Task GetDayAsync_ExcludesNonFreeFleet_AndOutOfOrderEquipment()
    {
        await CreateBoatAsync("NotFree", freeFleet: false);
        await CreateBoatAsync("Broken", outOfOrder: true);
        var visible = await CreateBoatAsync("Visible");

        var result = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));

        var allBoatIds = result.BoatTypeGroups.SelectMany(g => g.Boats).Select(b => b.EquipmentId).ToList();
        allBoatIds.ShouldBe([visible.Id]);
    }

    [Fact]
    public async Task GetDayAsync_ReturnsReservations_OnlyForRequestedDate()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));
        await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 7), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        var result = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));

        var boatDto = result.BoatTypeGroups.SelectMany(g => g.Boats).Single(b => b.EquipmentId == boat.Id);
        boatDto.Reservations.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetDayAsync_ComputesCanEdit_BasedOnOwnershipAndOverride()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner", "owner@example.com");
        var other = await CreateUserAsync("other", "other@example.com");
        var overrideUser = await CreateUserAsync("override-user", "override-user@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var asOwner = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));
        asOwner.BoatTypeGroups.SelectMany(g => g.Boats).SelectMany(b => b.Reservations).Single().CanEdit.ShouldBeTrue();

        CurrentUserId = other.Id;
        CurrentServiceUserId = other.Id;
        var asOther = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));
        asOther.BoatTypeGroups.SelectMany(g => g.Boats).SelectMany(b => b.Reservations).Single().CanEdit.ShouldBeFalse();

        CurrentUserId = overrideUser.Id;
        CurrentServiceUserId = overrideUser.Id;
        CurrentUserPermissions.Add(BluePermission.MaterialPlannerOverride);
        var asOverride = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));
        asOverride.BoatTypeGroups.SelectMany(g => g.Boats).SelectMany(b => b.Reservations).Single().CanEdit.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_CreatesReservation_OwnedByCurrentUser()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        var result = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        result.OwnerUserId.ShouldBe(user.Id);
        result.CustomLabel.ShouldBeNull();
        result.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenOverlappingSameBoatAndDate()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 30), new TimeOnly(11, 30))));
    }

    [Fact]
    public async Task CreateAsync_DoesNotConflict_AcrossDifferentBoatsOrDates()
    {
        var boatA = await CreateBoatAsync("BoatA");
        var boatB = await CreateBoatAsync("BoatB");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await _sut.CreateAsync(new CreateMaterialReservationRequest(boatA.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        await Should.NotThrowAsync(() =>
            _sut.CreateAsync(new CreateMaterialReservationRequest(boatB.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0))));
        await Should.NotThrowAsync(() =>
            _sut.CreateAsync(new CreateMaterialReservationRequest(boatA.Id, new DateOnly(2026, 7, 7), new TimeOnly(10, 0), new TimeOnly(11, 0))));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStartIsNotBeforeEnd()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(11, 0), new TimeOnly(10, 0))));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEquipmentIsNotBookable()
    {
        var boat = await CreateBoatAsync("NotFree", freeFleet: false);
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0))));
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_AllowsOwner_ToMoveReservation()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        var updated = await _sut.UpdateAsync(created.Id, new UpdateMaterialReservationRequest(new TimeOnly(12, 0), new TimeOnly(13, 0)));

        updated.StartTime.ShouldBe(new TimeOnly(12, 0));
        updated.EndTime.ShouldBe(new TimeOnly(13, 0));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNonOwnerWithoutOverride()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner2", "owner2@example.com");
        var other = await CreateUserAsync("other2", "other2@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = other.Id;
        CurrentServiceUserId = other.Id;
        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.UpdateAsync(created.Id, new UpdateMaterialReservationRequest(new TimeOnly(12, 0), new TimeOnly(13, 0))));
    }

    [Fact]
    public async Task UpdateAsync_AllowsOverrideHolder_ToMoveOthersReservation()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner3", "owner3@example.com");
        var overrideUser = await CreateUserAsync("override3", "override3@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = overrideUser.Id;
        CurrentServiceUserId = overrideUser.Id;
        CurrentUserPermissions.Add(BluePermission.MaterialPlannerOverride);

        var updated = await _sut.UpdateAsync(created.Id, new UpdateMaterialReservationRequest(new TimeOnly(12, 0), new TimeOnly(13, 0)));
        updated.StartTime.ShouldBe(new TimeOnly(12, 0));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenMovingIntoAnotherReservationsRange()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;
        await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(14, 0), new TimeOnly(15, 0)));
        var moving = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(9, 0), new TimeOnly(10, 0)));

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.UpdateAsync(moving.Id, new UpdateMaterialReservationRequest(new TimeOnly(14, 30), new TimeOnly(15, 30))));
    }

    [Fact]
    public async Task UpdateAsync_DoesNotFalselyConflictWithItself()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        await Should.NotThrowAsync(() =>
            _sut.UpdateAsync(created.Id, new UpdateMaterialReservationRequest(new TimeOnly(10, 15), new TimeOnly(11, 15))));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNotFound()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), new UpdateMaterialReservationRequest(new TimeOnly(10, 0), new TimeOnly(11, 0))));
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_AllowsOwner_AndSoftDeletes()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        await _sut.DeleteAsync(created.Id);

        var result = await _sut.GetDayAsync(new DateOnly(2026, 7, 6));
        result.BoatTypeGroups.SelectMany(g => g.Boats).SelectMany(b => b.Reservations).ShouldBeEmpty();

        var raw = await Db.MaterialReservations.IgnoreQueryFilters().FirstAsync(r => r.Id == created.Id);
        raw.DeletedAt.ShouldNotBeNull();
        raw.DeletedByUserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNonOwnerWithoutOverride()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner4", "owner4@example.com");
        var other = await CreateUserAsync("other4", "other4@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = other.Id;
        CurrentServiceUserId = other.Id;
        await Should.ThrowAsync<BlueValidationException>(() => _sut.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_AllowsOverrideHolder()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner5", "owner5@example.com");
        var overrideUser = await CreateUserAsync("override5", "override5@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = overrideUser.Id;
        CurrentServiceUserId = overrideUser.Id;
        CurrentUserPermissions.Add(BluePermission.MaterialPlannerOverride);
        await Should.NotThrowAsync(() => _sut.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNotFound()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    // -------------------------------------------------------------------------
    // SetLabelAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetLabelAsync_AllowsOwner_ToSetAndClearLabel()
    {
        var boat = await CreateBoatAsync("Single");
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        var labeled = await _sut.SetLabelAsync(created.Id, new SetMaterialReservationLabelRequest("Team A training"));
        labeled.CustomLabel.ShouldBe("Team A training");

        var cleared = await _sut.SetLabelAsync(created.Id, new SetMaterialReservationLabelRequest(null));
        cleared.CustomLabel.ShouldBeNull();
    }

    [Fact]
    public async Task SetLabelAsync_AllowsOverrideHolder()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner6", "owner6@example.com");
        var overrideUser = await CreateUserAsync("override6", "override6@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = overrideUser.Id;
        CurrentServiceUserId = overrideUser.Id;
        CurrentUserPermissions.Add(BluePermission.MaterialPlannerOverride);
        var labeled = await _sut.SetLabelAsync(created.Id, new SetMaterialReservationLabelRequest("Override label"));
        labeled.CustomLabel.ShouldBe("Override label");
    }

    [Fact]
    public async Task SetLabelAsync_Throws_WhenNonOwnerWithoutOverride()
    {
        var boat = await CreateBoatAsync("Single");
        var owner = await CreateUserAsync("owner7", "owner7@example.com");
        var other = await CreateUserAsync("other7", "other7@example.com");

        CurrentUserId = owner.Id;
        CurrentServiceUserId = owner.Id;
        var created = await _sut.CreateAsync(new CreateMaterialReservationRequest(boat.Id, new DateOnly(2026, 7, 6), new TimeOnly(10, 0), new TimeOnly(11, 0)));

        CurrentUserId = other.Id;
        CurrentServiceUserId = other.Id;
        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.SetLabelAsync(created.Id, new SetMaterialReservationLabelRequest("Nope")));
    }

    [Fact]
    public async Task SetLabelAsync_Throws_WhenNotFound()
    {
        var user = await CreateUserAsync();
        CurrentUserId = user.Id;
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() =>
            _sut.SetLabelAsync(Guid.NewGuid(), new SetMaterialReservationLabelRequest("x")));
    }

    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private async Task<Equipment> CreateBoatAsync(
        string name,
        bool freeFleet = true,
        bool outOfOrder = false,
        string? typeName = null,
        bool createEquipmentType = true)
    {
        Guid? equipmentTypeId = null;
        if (createEquipmentType)
        {
            var type = new EquipmentType
            {
                Id = Guid.NewGuid(),
                Code = Guid.NewGuid().ToString("N")[..8],
                Name = typeName ?? "Skiff",
                Scull = true,
                Coxed = false,
                RowersCount = 1,
                IsBoat = true,
            };
            Db.EquipmentTypes.Add(type);
            equipmentTypeId = type.Id;
        }

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = name,
            EquipmentTypeId = equipmentTypeId,
            FreeFleet = freeFleet,
            OutOfOrder = outOfOrder,
        };
        Db.Equipment.Add(equipment);
        await Db.SaveChangesAsync();
        return equipment;
    }

    private async Task<Equipment> CreateBoatAsync(string name, EquipmentType? equipmentType)
    {
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = name,
            EquipmentTypeId = equipmentType?.Id,
            FreeFleet = true,
            OutOfOrder = false,
        };
        Db.Equipment.Add(equipment);
        await Db.SaveChangesAsync();
        return equipment;
    }
}
