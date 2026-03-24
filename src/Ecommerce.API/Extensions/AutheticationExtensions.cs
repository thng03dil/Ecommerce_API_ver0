using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Ecommerce.API.Extensions
{
    public static class AutheticationExtensions
    {
        public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
        {

            //register JwtSettings
            services.Configure<JwtSettings>(
            configuration.GetSection("Jwt"));


            //configure JWT in swagger and swagger global header.
            var secret = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new Exception("JWT Key is missing (env: Jwt__Key)");
            }

            if (secret.Length < 32)
            {
                throw new Exception("JWT Key must be at least 32 characters");
            }

            var key = Encoding.UTF8.GetBytes(secret);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // ================= TOKEN VALIDATION =================
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.Zero,

                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // ================= EVENTS =================
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");

                        logger.LogWarning(
                            context.Exception,
                            "JWT Bearer validation failed: {FailureType} Path={Path}",
                            context.Exception.GetType().Name,
                            context.HttpContext.Request.Path);

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        if (context.AuthenticateFailure is SecurityTokenExpiredException)
                            throw new UnauthorizedException("Token has expired");

                        if (context.AuthenticateFailure is SecurityTokenInvalidSignatureException)
                            throw new UnauthorizedException("Invalid token signature");

                        if (context.AuthenticateFailure is SecurityTokenException)
                            throw new UnauthorizedException("Invalid token");

                        throw new UnauthorizedException(
                            "Authentication failed: You are not authorized to access this resource.");
                    },

                    OnForbidden = context =>
                    {
                        throw new ForbiddenException("Access denied: You do not have permission");
                    }
                };
            });

            return services;
        }
        }
}
