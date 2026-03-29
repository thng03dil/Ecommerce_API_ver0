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

        /// <summary>Admin role: UI shows full access; <see cref="Permissions"/> empty.</summary>
        public bool FullAccess { get; set; }

        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }
}
