using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Application.Common.Caching
{
    public static class CacheKeyGenerator
    {
        public static string Product(int id) => $"product:{id}";
        public static string ProductVersionKey() => "product:version";
        public static string ProductList(string version, int page, int size, string filterHash) =>
            string.IsNullOrEmpty(filterHash)
                ? $"product:list:v{version}:{page}:{size}"
                : $"product:list:v{version}:{page}:{size}:{filterHash}";

        public static string Category(int id) => $"category:{id}";
        public static string CategoryVersionKey() => "category:version";
        public static string CategoryList(string version, int page, int size, string filterHash) =>
            string.IsNullOrEmpty(filterHash)
                ? $"category:list:v{version}:{page}:{size}"
                : $"category:list:v{version}:{page}:{size}:{filterHash}";

        public static string User(int id) => $"user:{id}";
        public static string UserVersionKey() => "user:version";
        public static string UserList(string version, int page, int size) =>
            $"user:list:v{version}:{page}:{size}";

        public static string Role(int id) => $"role:{id}";
        public static string RoleVersionKey() => "role:version";
        public static string RoleList(string version, int page, int size) =>
            $"role:list:v{version}:{page}:{size}";

        public static string PermissionVersionKey() => "permission:version";
        public static string PermissionList(string version, int page, int size) =>
            $"permission:list:v{version}:{page}:{size}";

        public static string GetEntityKey<T>(object id) => $"{typeof(T).Name}:{id}";

        public static string GetListKey<T>() => $"{typeof(T).Name}:List";

        public static string BlacklistToken(string tokenHash)
        { 
        return $"Blacklist:Token:{tokenHash}";
        }
        public static string UserTokenVersion(int userId)
         { 
               return $"User:TokenVersion:{userId}";
        }

        /// <summary>Key cache session; sessionVersion khớp User.SessionVersion (JWT claim sv).</summary>
        public static string AuthSession(int userId, int sessionVersion) =>
            $"auth:session:user:{userId}:v{sessionVersion}";

        public static string LoginFailure(string normalizedEmail) =>
            $"auth:loginfail:{normalizedEmail.ToLowerInvariant()}";
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
