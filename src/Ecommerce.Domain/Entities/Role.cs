using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
   public class Role : BaseEntity
    {

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Legacy seed flag. Built-in protection uses role names Admin and User in application logic.</summary>
        public bool IsSystem { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
