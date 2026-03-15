using BCrypt.Net;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.SecurityHelpers
{

    // Bỏ static ở class
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
  