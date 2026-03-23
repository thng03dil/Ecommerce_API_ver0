using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Infrastructure.SecurityHelpers
{
    public class SecurityFingerprintHelper : ISecurityFingerprintHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthSecuritySettings _settings;

        public SecurityFingerprintHelper(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuthSecuritySettings> settings)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings.Value;
        }

        public string GetClientIpAddress()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null)
                return string.Empty;

            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(first))
                    return first;
            }

            return ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }

        
        /// Fingerprint = HMAC-SHA256(secret, deviceId + "|" + ipAddress).
        
        public string ComputeFingerprint(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(_settings.FingerprintSecret))
                throw new InvalidOperationException("AuthSecurity:FingerprintSecret is not configured. Use User Secrets (AuthSecurity__FingerprintSecret) or environment variable.");

            var ip = GetClientIpAddress();
            var payload = $"{deviceId ?? string.Empty}|{ip}";
            var key = Encoding.UTF8.GetBytes(_settings.FingerprintSecret);
            var data = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}
