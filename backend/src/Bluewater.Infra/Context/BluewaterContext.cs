using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Infra.Context;

public class BluewaterContext : IdentityDbContext<BlueUser, BlueRole, Guid>
{
    public BluewaterContext(DbContextOptions<BluewaterContext> options) : base(options) { }

    
    public DbSet<BlueSeason> Seasons => Set<BlueSeason>();
    public DbSet<BlueAppSettings> AppSettings => Set<BlueAppSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


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

    }
}