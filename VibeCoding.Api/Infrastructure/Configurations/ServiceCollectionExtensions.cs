using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Background;
using VibeCoding.Api.Application.Services;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Infrastructure.BackgroundJobs;
using VibeCoding.Api.Infrastructure.Data;
using VibeCoding.Api.Infrastructure.Options;
using VibeCoding.Api.Infrastructure.Redis;

namespace VibeCoding.Api.Infrastructure.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresSection = configuration.GetRequiredSection(PostgresOptions.SectionName);
        services.Configure<PostgresOptions>(postgresSection);
        var postgresOptions = postgresSection.Get<PostgresOptions>()
            ?? throw new InvalidOperationException("Postgres configuration missing");

        services.AddDbContext<ApplicationDbContext>((_, options) =>
        {
            options.UseNpgsql(postgresOptions.ConnectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                builder.EnableRetryOnFailure();
            });
            options.EnableDetailedErrors(postgresOptions.EnableDetailedErrors);
            if (postgresOptions.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<UserManager<ApplicationUser>>();
        services.AddScoped<RoleManager<ApplicationRole>>();

        return services;
    }

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetRequiredSection(JwtOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetRequiredSection(AzureBlobOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetRequiredSection(RedisOptions.SectionName));
        services.Configure<TelemetryOptions>(configuration.GetRequiredSection(TelemetryOptions.SectionName));
        services.Configure<PostgresOptions>(configuration.GetRequiredSection(PostgresOptions.SectionName));
        return services;
    }

    public static IServiceCollection AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
    {
        var redisOptions = configuration.GetRequiredSection(RedisOptions.SectionName).Get<RedisOptions>()
            ?? throw new InvalidOperationException("Redis configuration missing");
        var blobOptions = configuration.GetRequiredSection(AzureBlobOptions.SectionName).Get<AzureBlobOptions>()
            ?? throw new InvalidOperationException("Azure blob configuration missing");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(redisOptions.ConnectionString, true);
            config.AbortOnConnectFail = false;
            config.ClientName = redisOptions.InstanceName;
            return ConnectionMultiplexer.Connect(config);
        });
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });
        services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(blobOptions.ConnectionString));
        services.AddSingleton<IBackgroundJobQueue>(_ => new BackgroundJobQueue());
        services.AddHostedService<BackgroundJobWorker>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddSingleton<IDistributedLockManager, RedisDistributedLockManager>();

        return services;
    }
}