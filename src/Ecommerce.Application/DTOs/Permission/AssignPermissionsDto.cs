using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Role
{

    public class AssignPermissionsDto
    {
        public int RoleId { get; set; }
        /// <summary>
        /// Tra cứu ID:
        /// - Products: 1-view, 2-viewbyid, 3-create, 4-update, 5-delete
        /// - Categories: 6-view, 7-viewbyid, 8-create, 9-update, 10-delete
        /// - Users: 11-view, 12-viewbyid, 13-update, 14-delete
        /// - Roles: 15-view, 16-viewbyid, 17-create, 18-update, 19-delete
        /// </summary>
        /// <example>[1, 2, 3]</example>
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}