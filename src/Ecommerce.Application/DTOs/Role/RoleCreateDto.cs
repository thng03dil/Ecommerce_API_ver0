using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Role
{
    public class RoleCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
