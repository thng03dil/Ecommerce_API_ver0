using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
using System.Security.Claims;

namespace Ecommerce.API.Middleware
{
    /// <summary>
    /// Intercepts every [Authorize] request: JTI blacklist check, fingerprint validation, reject legacy tokens.
    /// </summary>
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IJwtService jwtService,
            ITokenBlacklistService tokenBlacklist,
            ISessionValidationService sessionValidation,
            ISecurityFingerprintHelper fingerprint)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                await _next(context);
                return;
            }

            var sid = context.User.FindFirst("sid")?.Value;
            var sv = context.User.FindFirst("sv")?.Value;
            var fp = context.User.FindFirst("fp")?.Value;

            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(sv) || string.IsNullOrEmpty(fp))
                throw new UnauthorizedException("Token is invalid or outdated. Please log in again.");

            var jti = context.User.FindFirst("jti")?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                var jtiHash = jwtService.HashToken(jti);
                var blacklisted = await tokenBlacklist.IsBlacklistedAsync(jtiHash);
                if (blacklisted)
                    throw new UnauthorizedException("Token has been revoked");
            }

            var deviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault() ?? string.Empty;
            var currentFingerprint = fingerprint.ComputeFingerprint(deviceId);

            await sessionValidation.EnsureAccessTokenSessionValidAsync(
                userId,
                sid,
                sv,
                fp,
                currentFingerprint);

            await _next(context);
        }
    }
}
