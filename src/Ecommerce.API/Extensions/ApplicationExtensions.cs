using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.SecurityHelpers;
using Ecommerce.Infrastructure.Services;

namespace Ecommerce.API.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<IUserSessionInvalidationService, UserSessionInvalidationService>();
            services.AddScoped<ISessionValidationService, SessionValidationService>();
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
            return services;
        }
    }
}
