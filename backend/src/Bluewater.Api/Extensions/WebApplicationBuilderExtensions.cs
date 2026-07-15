using System.Text;
using Bluewater.Api.Authorization;
using Bluewater.Api.Options;
using Bluewater.Core.Options;
using Bluewater.Core.Services;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Validators;
using Bluewater.Domain.Models;
using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services;
using Bluewater.Infra.Services.Abstractions;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TokenOptions = Bluewater.Infra.Options.TokenOptions;

namespace Bluewater.Api.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddBluewater(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<TokenOptions>()
            .Bind(builder.Configuration.GetSection("Jwt"))
            .ValidateOnStart();

        builder.Services.AddOptions<CookieAuthOptions>()
            .Bind(builder.Configuration.GetSection("Cookie"))
            .ValidateOnStart();

        builder.Services.AddOptions<DatabaseOptions>()
            .Bind(builder.Configuration.GetSection("Database"))
            .ValidateOnStart();

        builder.Services.AddOptions<CorsOptions>()
            .Bind(builder.Configuration.GetSection("Cors"))
            .ValidateOnStart();

        builder.Services.AddOptions<LocalFileStorageOptions>()
            .Bind(builder.Configuration.GetSection("FileStorage:Local"))
            .ValidateOnStart();

        builder.Services.AddOptions<SeedingOptions>()
            .Bind(builder.Configuration.GetSection("Seeding"))
            .ValidateOnStart();

        builder.Services.AddOptions<MailOptions>()
            .Bind(builder.Configuration.GetSection("Mail"))
            .ValidateOnStart();

        builder.AddDatabase();
        builder.AddBlueCors();
        builder.AddMailAndBackgroundJobs();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Default Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            if (builder.Environment.IsDevelopment())
            {
                options.Password.RequiredLength = 4;    
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
            }
        });
        
        builder.Services.AddIdentityCore<BlueUser>()
            .AddRoles<BlueRole>()
            .AddEntityFrameworkStores<BluewaterContext>()
            .AddDefaultTokenProviders();
        

        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<TokenOptions>() ?? new TokenOptions();
        var cookieOptions = builder.Configuration.GetSection("Cookie").Get<CookieAuthOptions>() ?? new CookieAuthOptions();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token) &&
                            context.Request.Cookies.TryGetValue(cookieOptions.AccessTokenCookieName, out var cookieToken) &&
                            !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<CurrentUserService>();
        builder.Services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
        builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserService>());
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ICookieAuthService, CookieAuthService>();
        builder.Services.AddScoped<IUserGroupCategoryService, UserGroupCategoryService>();
        builder.Services.AddScoped<IUserGroupCategoryRoleService, UserGroupCategoryRoleService>();
        builder.Services.AddScoped<IUserGroupService, UserGroupService>();
        builder.Services.AddScoped<IUserGroupInstanceService, UserGroupInstanceService>();
        builder.Services.AddScoped<IUserGroupMembershipService, UserGroupMembershipService>();
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<INewsService, NewsService>();
        builder.Services.AddScoped<INewsIconService, NewsIconService>();
        builder.Services.AddScoped<IAgendaService, AgendaService>();
        builder.Services.AddScoped<ISeasonService, SeasonService>();
        builder.Services.AddScoped<IExamTypeService, ExamTypeService>();
        builder.Services.AddScoped<IUserExamService, UserExamService>();
        builder.Services.AddScoped<IManufacturerService, ManufacturerService>();
        builder.Services.AddScoped<IFleetEquipmentTypeService, FleetEquipmentTypeService>();
        builder.Services.AddScoped<IOarSetService, OarSetService>();
        builder.Services.AddScoped<IEquipmentService, EquipmentService>();
        builder.Services.AddScoped<IMemberClusterService, MemberClusterService>();
        builder.Services.AddScoped<ISignupCategoryService, SignupCategoryService>();
        builder.Services.AddScoped<ISignupService, SignupService>();
        builder.Services.AddScoped<IOutingService, OutingService>();
        builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
        builder.Services.AddScoped<IMaterialReservationService, MaterialReservationService>();
        builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
        builder.Services.AddScoped<Bluewater.Core.Services.Mail.IMergeTokenRenderer, Bluewater.Core.Services.Mail.MergeTokenRenderer>();
        builder.Services.AddScoped<Bluewater.Core.Services.Mail.IMailContentRenderer, Bluewater.Core.Services.Mail.MailContentRenderer>();
        builder.Services.AddScoped<IMailLayoutService, MailLayoutService>();
        builder.Services.AddScoped<IMailTemplateService, MailTemplateService>();
        builder.Services.AddScoped<IMailService, MailService>();
        builder.Services.AddScoped<Bluewater.Core.Services.Mail.TransactionalMailJob>();
        builder.Services.AddScoped<IMailingTargetResolverService, MailingTargetResolverService>();
        builder.Services.AddScoped<IMailingService, MailingService>();
        builder.Services.AddScoped<Bluewater.Core.Services.Mail.MailingRecipientSendJob>();
        builder.Services.AddScoped<Bluewater.Core.Services.Mail.MailProofSendJob>();
        builder.Services.AddScoped<IMailTrackingService, MailTrackingService>();
        builder.Services.AddOptions<SignupOptions>()
            .Bind(builder.Configuration.GetSection("Signup"))
            .ValidateOnStart();
        builder.Services.AddValidatorsFromAssemblyContaining<UpsertNewsPostRequestValidator>();

        return builder;
    }

    private static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = dbOptions.BuildConnectionString();

        builder.Services.AddDbContext<BluewaterContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<BluewaterContextSeeder>();

        return builder;
    }

    private static WebApplicationBuilder AddMailAndBackgroundJobs(this WebApplicationBuilder builder)
    {
        var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = dbOptions.BuildConnectionString();

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
        builder.Services.AddHangfireServer();

        // Mail jobs handle their own failure classification (bounce vs. transient) and don't
        // want Hangfire silently re-attempting a send behind their backs — a failed job should
        // surface as failed, not retry.
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

        builder.Services.AddScoped<IMailTransportService, MailTransportService>();

        return builder;
    }

    private static WebApplicationBuilder AddBlueCors(this WebApplicationBuilder builder)
    {
        var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });

        return builder;
    }
}