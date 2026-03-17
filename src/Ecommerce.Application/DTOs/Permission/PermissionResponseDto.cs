using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Permission
{
    public class PermissionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Entity { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
