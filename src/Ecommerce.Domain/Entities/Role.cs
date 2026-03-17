using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
   public class Role : BaseEntity
    {

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
