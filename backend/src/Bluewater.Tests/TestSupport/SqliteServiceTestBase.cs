using Bluewater.Core.Options;
using Bluewater.Core.Services;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Validators;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services;
using Bluewater.Infra.Services.Abstractions;
using FluentValidation;
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
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly FakeBackgroundJobClient _fakeBackgroundJobClient = new();
    private readonly string _fileStorageRootPath;

    protected BluewaterContext Db { get; }
    protected UserManager<BlueUser> UserManager { get; }
    protected string FileStorageRootPath => _fileStorageRootPath;
    protected FakeBackgroundJobClient BackgroundJobClient => _fakeBackgroundJobClient;

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

    /// <summary>
    /// The user ID surfaced to services via <see cref="ICurrentUserService.Id"/>.
    /// Defaults to <see cref="Guid.Empty"/>. Set this alongside <see cref="CurrentUserId"/>
    /// when testing services that read both the audit actor and the acting user identity.
    /// </summary>
    protected Guid CurrentServiceUserId
    {
        get => _currentUserService.Id;
        set => _currentUserService.Id = value;
    }

    /// <summary>
    /// Permissions granted to the acting user for service-level permission checks
    /// (e.g. INewsService's members-only visibility filtering). Empty by default.
    /// </summary>
    protected HashSet<BluePermission> CurrentUserPermissions => _currentUserService.Permissions;

    protected SqliteServiceTestBase()
    {
        // The in-memory database only lives as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<ICurrentUserAccessor>(_currentUserAccessor);
        services.AddSingleton<ICurrentUserService>(_currentUserService);
        services.AddDbContext<BluewaterContext>(options => options.UseSqlite(_connection));
        services.Configure<IdentityOptions>(options =>
        {
            // Mirrors the Development password relaxation in WebApplicationBuilderExtensions.AddBluewater(),
            // which BluewaterContextSeeder relies on to create the admin user with the "admin" password.
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
        });
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
        services.Configure<MailOptions>(o => o.PublicBaseUrl = "https://test.example.com");
        services.AddScoped<TokenService>();
        services.AddSingleton<ICookieAuthService, TestCookieAuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserGroupCategoryService, UserGroupCategoryService>();
        services.AddScoped<IUserGroupCategoryRoleService, UserGroupCategoryRoleService>();
        services.AddScoped<IUserGroupService, UserGroupService>();
        services.AddScoped<IUserGroupInstanceService, UserGroupInstanceService>();
        services.AddScoped<IUserGroupMembershipService, UserGroupMembershipService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<INewsIconService, NewsIconService>();
        services.AddScoped<IAgendaService, AgendaService>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IMemberClusterService, MemberClusterService>();
        services.AddScoped<ISignupCategoryService, SignupCategoryService>();
        services.AddScoped<ISignupService, SignupService>();
        services.Configure<SignupOptions>(o => o.HideAfterDays = 14);
        services.AddScoped<IOutingService, OutingService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IMaterialReservationService, MaterialReservationService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IExamTypeService, ExamTypeService>();
        services.AddScoped<IUserExamService, UserExamService>();
        services.AddScoped<Bluewater.Core.Services.Mail.IMergeTokenRenderer, Bluewater.Core.Services.Mail.MergeTokenRenderer>();
        services.AddScoped<Bluewater.Core.Services.Mail.IMailContentRenderer, Bluewater.Core.Services.Mail.MailContentRenderer>();
        services.AddScoped<IMailLayoutService, MailLayoutService>();
        services.AddScoped<IMailTemplateService, MailTemplateService>();
        services.AddSingleton(_fakeBackgroundJobClient);
        services.AddSingleton<Hangfire.IBackgroundJobClient>(_fakeBackgroundJobClient);
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<Bluewater.Core.Services.Mail.TransactionalMailJob>();
        services.AddScoped<IMailingTargetResolverService, MailingTargetResolverService>();
        services.AddScoped<IMailingService, MailingService>();
        services.AddScoped<Bluewater.Core.Services.Mail.MailingRecipientSendJob>();
        services.AddScoped<Bluewater.Core.Services.Mail.MailProofSendJob>();
        services.AddScoped<IMailTrackingService, MailTrackingService>();
        services.AddScoped<BluewaterContextSeeder>();
        services.AddValidatorsFromAssemblyContaining<UpsertNewsPostRequestValidator>();

        _fileStorageRootPath = Path.Combine(Path.GetTempPath(), "bluewater-tests", Guid.NewGuid().ToString());
        services.Configure<LocalFileStorageOptions>(o => o.RootPath = _fileStorageRootPath);
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

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

        if (Directory.Exists(_fileStorageRootPath))
        {
            Directory.Delete(_fileStorageRootPath, recursive: true);
        }
    }

    private class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid? UserId { get; set; }
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public HashSet<BluePermission> Permissions { get; } = [];

        public bool HasPermission(BluePermission permission) => Permissions.Contains(permission);
    }

    private class TestCookieAuthService : ICookieAuthService
    {
        public void SetAuthCookies(Bluewater.Core.Dto.AuthResponse response) { }
        public void ClearAuthCookies() { }
        public string? GetRefreshTokenFromCookie() => null;
    }
}
