using Bluewater.Core.Dto.Profile;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class UserProfileService : IUserProfileService
{
    private readonly BluewaterContext _db;
    private readonly IUserGroupMembershipService _membershipService;

    public UserProfileService(BluewaterContext db, IUserGroupMembershipService membershipService)
    {
        _db = db;
        _membershipService = membershipService;
    }

    public async Task<UserProfileDto> GetAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new BlueNotFoundException($"User '{userId}' was not found.");

        var groups = await _membershipService.GetGroupsForUserAsync(userId);

        return new UserProfileDto(user.Id, user.Firstname, user.SurnamePrefix, user.Surname, user.Fullname, groups);
    }
}
