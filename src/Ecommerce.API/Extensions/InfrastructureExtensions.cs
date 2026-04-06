using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.RedisCaching;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.SecurityHelpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

namespace Ecommerce.API.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddCaching(configuration)
            .AddRepositories()
            .AddSecurity();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// Đọc <c>Caching:Provider</c> để chọn backend:
    /// <list type="bullet">
    ///   <item><c>None</c>  — <see cref="NoOpCacheService"/> (direct-DB, không cần Redis/memory).</item>
    ///   <item><c>Memory</c> hoặc trống — <see cref="RedisCacheService"/> + <c>DistributedMemoryCache</c> (local dev, multiplexer null).</item>
    ///   <item><c>Redis</c>  — <see cref="RedisCacheService"/> + StackExchange.Redis (production / có Redis server).</item>
    /// </list>
    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        var provider = (configuration["Caching:Provider"] ?? string.Empty).Trim();
        var redisConn = (configuration["Redis:ConnectionString"] ?? string.Empty).Trim();
        var instanceName = configuration["Redis:InstanceName"] ?? "Ecommerce:";


        // TRƯỜNG HỢP 1: Tắt hoàn toàn Cache (Dùng cho Docker/Test chạy thẳng DB)
        if (string.Equals(provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICacheService, NoOpCacheService>();
            Log.Information("Cache Provider: None");
            return services;
        }

        var useRedis =
            string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(redisConn);

        // TRƯỜNG HỢP 2: Sử dụng Redis (Có L1 là Memory dự phòng)
        if (useRedis)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var config = ConfigurationOptions.Parse(redisConn, true);
                config.AbortOnConnectFail = false;
                config.ConnectTimeout = 300;  // Fast timeout để trigger L1 nhanh
                return ConnectionMultiplexer.Connect(config);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = instanceName;
            });

            services.Configure<RedisCacheOptions>(o => 
            {
                o.DistributedCacheKeyPrefix = instanceName;
            });

            services.AddSingleton<ICacheService, RedisCacheService>();
            Log.Information("Cache Provider: Redis");
            return services;
        }
        else 
        {
        // TRƯỜNG HỢP 3: Sử dụng In-Memory (Cấu hình "Memory" hoặc Fallback khi Redis thiếu ConnectionString)
        services.AddDistributedMemoryCache();
        services.Configure<RedisCacheOptions>(o =>
        {
            o.DistributedCacheKeyPrefix = string.Empty;
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        Log.Information("Cache Provider: Memory Only");
    }
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepo, CategoryRepo>();
        services.AddScoped<IProductRepo, ProductRepo>();
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IRoleRepo, RoleRepo>();
        services.AddScoped<IPermissionRepo, PermissionRepo>();
        services.AddScoped<IOrderRepo, OrderRepo>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISecurityFingerprintHelper, SecurityFingerprintHelper>();
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
