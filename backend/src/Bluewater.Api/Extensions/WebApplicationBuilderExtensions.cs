using System.Text;
using Bluewater.Api.Authorization;
using Bluewater.Api.Options;
using Bluewater.Core.Services;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Validators;
using Bluewater.Domain.Models;
using Bluewater.Infra.Context;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services;
using Bluewater.Infra.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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

        builder.AddDatabase();
        builder.AddBlueCors();

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
        builder.Services.AddScoped<IUserGroupService, UserGroupService>();
        builder.Services.AddScoped<IUserGroupInstanceService, UserGroupInstanceService>();
        builder.Services.AddScoped<IUserGroupMembershipService, UserGroupMembershipService>();
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddScoped<INewsService, NewsService>();
        builder.Services.AddScoped<INewsIconService, NewsIconService>();
        builder.Services.AddScoped<IAgendaService, AgendaService>();
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
        builder.Services.AddValidatorsFromAssemblyContaining<UpsertNewsPostRequestValidator>();

        return builder;
    }

    private static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = dbOptions.Host,
            Port = dbOptions.Port,
            Database = dbOptions.Database,
            Username = dbOptions.Username,
            Password = dbOptions.Password
        }.ConnectionString;

        builder.Services.AddDbContext<BluewaterContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<BluewaterContextSeeder>();

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