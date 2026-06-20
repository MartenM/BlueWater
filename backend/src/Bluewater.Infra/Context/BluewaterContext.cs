using System.Reflection;
using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Auth;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditing()
    {
        var userId = _currentUserAccessor?.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

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
                    break;
            }
        }
    }
}