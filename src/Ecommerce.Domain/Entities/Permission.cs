using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
    public class Permission : BaseEntity
    {

        public string Name { get; set; } = null!;
        public string Entity { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsSystem { get; set; } = false;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
