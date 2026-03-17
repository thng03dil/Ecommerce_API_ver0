using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce.Application.DTOs.Permission;

namespace Ecommerce.Application.DTOs.Role
{
    public class RoleWithPermissionsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }
}
