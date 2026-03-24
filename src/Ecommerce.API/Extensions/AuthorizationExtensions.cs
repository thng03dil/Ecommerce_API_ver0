using Ecommerce.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.API.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
        {
            //register custom authorization handler and policy provider
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

            return services;
        }
    }
}
