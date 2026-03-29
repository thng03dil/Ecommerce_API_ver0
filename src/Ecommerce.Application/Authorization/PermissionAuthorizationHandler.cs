using System.Security.Claims;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId) || userId <= 0)
            return;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // 1) Supreme role from JWT — no DB
        var ridClaim = PermissionAuthConstants.GetRoleIdClaim(context.User);
        var roleNameClaim = PermissionAuthConstants.GetRoleNameClaim(context.User);
        if (PermissionAuthConstants.IsSupremeFromJwtClaims(ridClaim, roleNameClaim))
        {
            context.Succeed(requirement);
            return;
        }

        var userRepo = httpContext.RequestServices.GetRequiredService<IUserRepo>();
        var rolePerm = httpContext.RequestServices.GetRequiredService<IRolePermissionService>();

        var ctx = await userRepo.GetRoleContextForAuthAsync(userId, httpContext.RequestAborted);
        if (ctx == null)
            return;

        if (PermissionAuthConstants.IsSupremeRole(ctx.Value.RoleId, ctx.Value.RoleName))
        {
            context.Succeed(requirement);
            return;
        }

        if (await rolePerm.RoleHasPermissionAsync(ctx.Value.RoleId, requirement.Permission, httpContext.RequestAborted))
            context.Succeed(requirement);
    }
}
