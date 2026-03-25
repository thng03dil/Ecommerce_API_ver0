using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.RedisCaching;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.SecurityHelpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

namespace Ecommerce.API.Extensions
{
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
        // ================= DATABASE =================
        private static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
                {
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    return services;
        }

        // ================= REDIS CONFIGURATIOGN =================
        private static IServiceCollection AddCaching(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConn = configuration["Redis:ConnectionString"];
            var instanceName = configuration["Redis:InstanceName"] ?? "Ecommerce:";

            if (!string.IsNullOrEmpty(redisConn))
            {
                // Register Multiplexer (Singleton) for advanced Redis ops inside RedisCacheService
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var config = ConfigurationOptions.Parse(redisConn, true);
                    config.AbortOnConnectFail = false;
                    return ConnectionMultiplexer.Connect(config);
                });

                // Register Distributed Cache
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                    options.InstanceName = instanceName;
                });
            }
            else
            { 
                // Fallback : use memory cache if redis is not configured
                services.AddDistributedMemoryCache();
                Log.Warning("Redis ConnectionString is missing. Using MemoryCache instead.");
            }

            services.Configure<RedisCacheOptions>(o =>
            {
                o.DistributedCacheKeyPrefix = string.IsNullOrEmpty(redisConn) ? string.Empty : instanceName;
            });

            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }
        // ================= REPOSITORY REGISTRATION =================
        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ICategoryRepo, CategoryRepo>();
            services.AddScoped<IProductRepo, ProductRepo>();
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
            services.AddScoped<IRoleRepo, RoleRepo>();
            services.AddScoped<IPermissionRepo, PermissionRepo>();
            services.AddScoped<IOrderRepo, OrderRepo>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static IServiceCollection AddSecurity( this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ISecurityFingerprintHelper, SecurityFingerprintHelper>();

            // JWT implementation 
            services.AddScoped<IJwtService, JwtService>();

            return services;
        }
    }
}
