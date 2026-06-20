using System.Linq.Expressions;
using System.Reflection;
using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Auth;
using Bluewater.Domain.Models.Files;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Bluewater.Infra.Context;

public class BluewaterContext : IdentityDbContext<BlueUser, BlueRole, Guid>
{
    private readonly ICurrentUserAccessor? _currentUserAccessor;

    public BluewaterContext(DbContextOptions<BluewaterContext> options, ICurrentUserAccessor? currentUserAccessor = null)
        : base(options)
    {
        _currentUserAccessor = currentUserAccessor;
    }


    public DbSet<BlueSeason> Seasons => Set<BlueSeason>();
    public DbSet<BlueAppSettings> AppSettings => Set<BlueAppSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserGroupCategory> UserGroupCategories => Set<UserGroupCategory>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupInstance> UserGroupInstances => Set<UserGroupInstance>();
    public DbSet<UserGroupInstanceMember> UserGroupInstanceMembers => Set<UserGroupInstanceMember>();
    public DbSet<UserGroupInstancePermission> UserGroupInstancePermissions => Set<UserGroupInstancePermission>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<BlueUser>(e =>
        {
            e.HasKey(x => x.Id);
            
            e.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .IsRequired();
            
            e.HasIndex(x => x.Email)
                .IsUnique();

            e.HasOne(x => x.ProfilePicture)
                .WithMany()
                .HasForeignKey(x => x.ProfilePictureFileId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.TokenHash)
                .IsUnique();

            e.HasOne<BlueUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<BlueSeason>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.StartDate)
                .IsUnique();
            
            e.HasIndex(x => x.EndDate)
                .IsUnique();
        });

        builder.Entity<BlueAppSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.LoginEnabled)
                .IsRequired();

            e.Property(x => x.CurrentSeasonId)
                .IsRequired();

            e.HasOne(x => x.CurrentSeason)
                .WithOne()
                .HasForeignKey<BlueAppSettings>(x => x.CurrentSeasonId);
        });

        builder.Entity<UserGroupCategory>(e =>
        {
            e.HasKey(x => x.Id);
        });

        builder.Entity<UserGroup>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.UserGroupCategory)
                .WithMany()
                .HasForeignKey(x => x.UserGroupCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserGroupInstance>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.UserGroupId, x.SeasonId })
                .IsUnique();

            e.HasOne(x => x.UserGroup)
                .WithMany()
                .HasForeignKey(x => x.UserGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Season)
                .WithMany()
                .HasForeignKey(x => x.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserGroupInstanceMember>(e =>
        {
            e.HasKey(x => new { x.UserGroupInstanceId, x.UserId });

            e.HasOne(x => x.UserGroupInstance)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.UserGroupInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserGroupInstancePermission>(e =>
        {
            e.HasKey(x => new { x.UserGroupInstanceId, x.Permission });

            e.HasOne(x => x.UserGroupInstance)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.UserGroupInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StoredFile>(e =>
        {
            e.HasKey(x => x.Id);
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                ApplySoftDeleteFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, [builder]);
            }
        }
    }

    private static readonly MethodInfo ApplySoftDeleteFilterMethod =
        typeof(BluewaterContext).GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : class, IAuditable
    {
        builder.Entity<TEntity>().HasQueryFilter(x => x.DeletedAt == null);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ReviveSoftDeletedRelations();
        var softDeleted = ApplyAuditing();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        DetachSoftDeleted(softDeleted);
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ReviveSoftDeletedRelationsAsync(cancellationToken);
        var softDeleted = ApplyAuditing();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        DetachSoftDeleted(softDeleted);
        return result;
    }

    /// <summary>
    /// IAuditableRelation entities (composite-key join/relation rows) keep their soft-deleted row
    /// under the same key. Re-adding under that key must revive the existing row instead of
    /// inserting a duplicate, which would violate the primary key.
    /// </summary>
    private void ReviveSoftDeletedRelations()
    {
        foreach (var entry in PendingRelationAdds())
        {
            ReviveIfSoftDeletedMethod
                .MakeGenericMethod(entry.Entity.GetType())
                .Invoke(null, [this, entry]);
        }
    }

    private async Task ReviveSoftDeletedRelationsAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in PendingRelationAdds())
        {
            var task = (Task)ReviveIfSoftDeletedAsyncMethod
                .MakeGenericMethod(entry.Entity.GetType())
                .Invoke(null, [this, entry, cancellationToken])!;
            await task;
        }
    }

    private List<EntityEntry<IAuditableRelation>> PendingRelationAdds() =>
        ChangeTracker.Entries<IAuditableRelation>()
            .Where(e => e.State == EntityState.Added)
            .ToList();

    private static readonly MethodInfo ReviveIfSoftDeletedMethod =
        typeof(BluewaterContext).GetMethod(nameof(ReviveIfSoftDeleted), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ReviveIfSoftDeletedAsyncMethod =
        typeof(BluewaterContext).GetMethod(nameof(ReviveIfSoftDeletedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void ReviveIfSoftDeleted<TEntity>(BluewaterContext context, EntityEntry entry)
        where TEntity : class, IAuditableRelation
    {
        var predicate = BuildKeyPredicate<TEntity>(context, entry);

        // Probe without tracking: the about-to-be-inserted entity is itself tracked as Added under
        // this same key, so a tracked query would resolve identity against it instead of the real
        // (soft-deleted) persisted row.
        var probe = context.Set<TEntity>().IgnoreQueryFilters().AsNoTracking().FirstOrDefault(predicate);
        if (probe == null || probe.DeletedAt == null)
        {
            return; // genuinely new row, or already-active under this key - a real conflict, let the PK violation surface
        }

        var newValues = entry.Entity;
        entry.State = EntityState.Detached; // free the key so the real row can be loaded and tracked instead

        var existing = context.Set<TEntity>().IgnoreQueryFilters().First(predicate);
        Revive(context, existing, newValues);
    }

    private static async Task ReviveIfSoftDeletedAsync<TEntity>(BluewaterContext context, EntityEntry entry, CancellationToken cancellationToken)
        where TEntity : class, IAuditableRelation
    {
        var predicate = BuildKeyPredicate<TEntity>(context, entry);

        var probe = await context.Set<TEntity>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        if (probe == null || probe.DeletedAt == null)
        {
            return;
        }

        var newValues = entry.Entity;
        entry.State = EntityState.Detached;

        var existing = await context.Set<TEntity>().IgnoreQueryFilters().FirstAsync(predicate, cancellationToken);
        Revive(context, existing, newValues);
    }

    private static void Revive<TEntity>(BluewaterContext context, TEntity existing, object newValues)
        where TEntity : class, IAuditableRelation
    {
        // CurrentValues.SetValues copies every matching scalar property by name, including
        // CreatedAt/CreatedByUserId off the brand-new (never-stamped) instance; preserve the
        // original creation audit values across the copy.
        var createdAt = existing.CreatedAt;
        var createdByUserId = existing.CreatedByUserId;

        context.Entry(existing).CurrentValues.SetValues(newValues);

        existing.CreatedAt = createdAt;
        existing.CreatedByUserId = createdByUserId;
        existing.DeletedAt = null;
        existing.DeletedByUserId = null;
    }

    private static Expression<Func<TEntity, bool>> BuildKeyPredicate<TEntity>(BluewaterContext context, EntityEntry entry)
        where TEntity : class
    {
        var keyProperties = context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;
        var parameter = Expression.Parameter(typeof(TEntity), "x");

        Expression? body = null;
        foreach (var keyProperty in keyProperties)
        {
            var currentValue = entry.Property(keyProperty.Name).CurrentValue;
            var left = Expression.Property(parameter, keyProperty.PropertyInfo!);
            var right = Expression.Constant(currentValue, keyProperty.ClrType);
            var equals = Expression.Equal(left, right);
            body = body == null ? equals : Expression.AndAlso(body, equals);
        }

        return Expression.Lambda<Func<TEntity, bool>>(body!, parameter);
    }

    private List<EntityEntry<IAuditable>> ApplyAuditing()
    {
        var userId = _currentUserAccessor?.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;
        var softDeleted = new List<EntityEntry<IAuditable>>();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedByUserId = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedByUserId = userId;
                    softDeleted.Add(entry);
                    break;
            }
        }

        return softDeleted;
    }

    /// <summary>
    /// Soft-deleted rows stay in the table under their original key, so leaving them tracked
    /// risks EF's navigation fixup re-attaching them to a collection navigation (e.g. via
    /// Include) on a later query in this same context, bypassing the DeletedAt query filter.
    /// Detaching forces any later read to go through a fresh, filtered query instead.
    /// </summary>
    private static void DetachSoftDeleted(List<EntityEntry<IAuditable>> softDeleted)
    {
        foreach (var entry in softDeleted)
        {
            entry.State = EntityState.Detached;
        }
    }
}