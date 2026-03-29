using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs.Auth
{
    public class UserMeResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        /// <summary>True for built-in Admin (full access; <see cref="Permissions"/> is empty).</summary>
        public bool FullAccess { get; set; }

        public List<string> Permissions { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}

