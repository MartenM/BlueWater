using Bluewater.Core.Services;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Infra.Context;
using Bluewater.Infra.Services;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TokenOptions = Bluewater.Infra.Options.TokenOptions;

namespace Bluewater.Tests.TestSupport;

/// <summary>
/// Base class for service tests backed by a real EF Core <see cref="BluewaterContext"/>
/// running against a fresh in-memory SQLite database. xUnit creates a new instance of the
/// test class per test method, so each test gets its own isolated database and connection.
/// </summary>
public abstract class SqliteServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly TestCurrentUserAccessor _currentUserAccessor = new();

    protected BluewaterContext Db { get; }
    protected UserManager<BlueUser> UserManager { get; }

    /// <summary>
    /// The acting user stamped onto audit fields (CreatedByUserId/UpdatedByUserId/DeletedByUserId)
    /// by <see cref="BluewaterContext"/>. Defaults to null (no current user); set before a SUT call
    /// to assert audit-stamping behavior.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get => _currentUserAccessor.UserId;
        set => _currentUserAccessor.UserId = value;
    }

    protected SqliteServiceTestBase()
    {
        // The in-memory database only lives as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<ICurrentUserAccessor>(_currentUserAccessor);
        services.AddDbContext<BluewaterContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<BlueUser>()
            .AddRoles<BlueRole>()
            .AddEntityFrameworkStores<BluewaterContext>()
            .AddDefaultTokenProviders();

        // Mirrors WebApplicationBuilderExtensions.AddBluewater() so SUTs are resolved through
        // DI rather than `new`'d up directly. Add a service here when its tests start needing it.
        services.Configure<TokenOptions>(o =>
        {
            o.Secret = "test-signing-secret-at-least-32-bytes-long!!";
            o.Issuer = "bluewater-tests";
            o.Audience = "bluewater-tests";
            o.ExpireTime = TimeSpan.FromMinutes(15);
        });
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserGroupCategoryService, UserGroupCategoryService>();
        services.AddScoped<IUserGroupService, UserGroupService>();
        services.AddScoped<IUserGroupInstanceService, UserGroupInstanceService>();
        services.AddScoped<IUserGroupMembershipService, UserGroupMembershipService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        Db = _scope.ServiceProvider.GetRequiredService<BluewaterContext>();
        // Builds the schema from the current EF model rather than the Npgsql-flavored
        // migrations, since SQLite can't run those directly.
        Db.Database.EnsureCreated();

        UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<BlueUser>>();
    }

    protected async Task<BlueUser> CreateUserAsync(string username = "test-user", string email = "test-user@example.com")
    {
        var user = new BlueUser { UserName = username, Email = email, EmailConfirmed = true };

        var result = await UserManager.CreateAsync(user, "Test123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    /// <summary>
    /// Creates a season and marks it as the current season in <see cref="BlueAppSettings"/>,
    /// which AuthService/UserGroupInstanceService/UserGroupMembershipService all rely on existing.
    /// </summary>
    protected async Task<BlueSeason> CreateCurrentSeasonAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var season = new BlueSeason
        {
            Id = Guid.NewGuid(),
            StartDate = startDate ?? new DateOnly(2025, 6, 1),
            EndDate = endDate ?? new DateOnly(2026, 5, 31)
        };
        Db.Seasons.Add(season);
        Db.AppSettings.Add(new BlueAppSettings { CurrentSeasonId = season.Id });
        await Db.SaveChangesAsync();

        return season;
    }

    protected T GetService<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    private class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid? UserId { get; set; }
    }
}
