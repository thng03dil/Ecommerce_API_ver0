using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Application.Common.Caching
{ 
    public static class CacheKeyGenerator
    {
        public static string Category(int id) => $"category:{id}";
        public static string CategoryVersionKey() => "category:version";
        public static string CategoryList(string version, int page, int size, string filterHash) =>
            string.IsNullOrEmpty(filterHash)
                ? $"category:list:v{version}:{page}:{size}"
                : $"category:list:v{version}:{page}:{size}:{filterHash}";

        /// <summary>Optional eviction key (e.g. after role reassignment); user list/detail are not cached.</summary>
        public static string User(int id) => $"user:{id}";

        public static string Role(int id) => $"role:{id}";
        public static string RoleVersionKey() => "role:version";
        public static string RoleList(string version, int page, int size) =>
            $"role:list:v{version}:{page}:{size}";

        public static string Permission(int id) => $"permission:{id}";
        public static string PermissionVersionKey() => "permission:version";
        public static string PermissionList(string version, int page, int size) =>
            $"permission:list:v{version}:{page}:{size}";

        public static string BlacklistToken(string tokenHash) =>
            $"Blacklist:Token:{tokenHash}";

        /// <summary>Single session key per user. Overwrites on every login / refresh.</summary>
        public static string AuthSession(int userId) =>
            $"auth:session:user:{userId}";

        /// <summary>Kept for backward-compat cache removal in tests; points to the single-key prefix.</summary>
        public static string AuthSessionUserPrefix(int userId) =>
            $"auth:session:user:{userId}";

        public static string LoginFailure(string normalizedEmail) =>
            $"auth:loginfail:{normalizedEmail.ToLowerInvariant()}";

        /// <summary>Cached lowercase permission names for a role (authorization).</summary>
        public static string RolePermissionNames(int roleId) =>
            $"auth:roleperm:{roleId}";

        public static string RolePermissionCachePrefix() =>
            "auth:roleperm:";
        /// <summary>Computes a short hash from an object for use in cache keys (e.g. filter JSON).</summary>
        public static string HashFilter(object? obj)
        {
            if (obj == null) return string.Empty;
            try
            {
                var json = JsonSerializer.Serialize(obj);
                return GenerateHash(json);
            }
            catch { return string.Empty; }
        }

        private static string GenerateHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);

            // Xoá kí tự đặc biệt Base64 để an toàn cho Redis Key
            return Convert.ToBase64String(hash)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
        }
    }
}
