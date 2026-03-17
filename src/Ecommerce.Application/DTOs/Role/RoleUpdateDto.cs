using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Ecommerce.Application.DTOs.Role
{
    public class RoleUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }

        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
    }
}
