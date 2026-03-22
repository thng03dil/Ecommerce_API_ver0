using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Infrastructure.SecurityHelpers
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly byte[] _keyBytes;

        public JwtService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;

            if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
                throw new Exception("JWT Key is missing from environment variables");

            if (_jwtSettings.Key.Length < 32)
                throw new Exception("JWT Key must be at least 32 characters");

            _keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        }

        public string GenerateAccessToken(
            User user,
            Guid sessionId,
            int sessionVersion,
            string fingerprint)
        {
            if (user.Role == null)
                throw new InvalidOperationException("User Role is not loaded when generating token. Please ensure Role and Permissions are included when fetching the User from the database.");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("sid", sessionId.ToString()),
                new Claim("sv", sessionVersion.ToString()),
                new Claim("fp", fingerprint ?? string.Empty),
            };

            if (user.Role?.RolePermissions != null)
            {
                var permissions = user.Role.RolePermissions
                    .Select(rp => rp.Permission.Name);

                foreach (var permissionName in permissions)
                {
                    claims.Add(new Claim("permissions", permissionName));
                }
            }

            var key = new SymmetricSecurityKey(_keyBytes);

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes)
                            .Replace("+", "")
                            .Replace("/", "")
                            .Replace("=", "");
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = new SymmetricSecurityKey(_keyBytes);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,

                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(
                token,
                tokenValidationParameters,
                out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        public string HashToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public TimeSpan? GetAccessTokenRemainingLifetime(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return null;

                var jwt = handler.ReadJwtToken(token);
                var rem = jwt.ValidTo - DateTime.UtcNow;
                return rem <= TimeSpan.Zero ? TimeSpan.Zero : rem;
            }
            catch
            {
                return null;
            }
        }
    }
}
