namespace Bluewater.Infra.Services.Abstractions;

/// <summary>
/// Minimal accessor for the acting user, consumed by <see cref="Bluewater.Infra.Context.BluewaterContext"/>
/// to stamp audit fields. Kept in Infra (rather than reusing Core's ICurrentUserService) since
/// Infra cannot depend on Core.
/// </summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
