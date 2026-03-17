using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
    public class Permission : BaseEntity
    {

        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Entity { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
