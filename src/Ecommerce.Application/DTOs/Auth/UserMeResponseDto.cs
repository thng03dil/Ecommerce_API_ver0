using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs.Auth
{
    public class UserMeResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}

