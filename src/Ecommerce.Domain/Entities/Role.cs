using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
   public class Role : BaseEntity
    {

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Vai trò hệ thống (Admin, User, …) — không cho phép xóa.</summary>
        public bool IsSystem { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
