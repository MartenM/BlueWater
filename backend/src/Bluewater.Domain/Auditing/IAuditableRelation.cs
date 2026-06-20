namespace Bluewater.Domain.Auditing;

/// <summary>
/// Marks an IAuditable entity whose primary key is fully caller-supplied and may collide with a
/// previously soft-deleted row (composite-key join/relation entities). BluewaterContext uses this
/// to scope the extra "does a soft-deleted row exist under this key" check to entities that need
/// it, instead of running it for every IAuditable add.
/// </summary>
public interface IAuditableRelation : IAuditable
{
}
