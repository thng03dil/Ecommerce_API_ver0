using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Permission
{
    public class PermissionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
