using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Ecommerce.Application.DTOs.Permission
{
    public class PermissionUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Permission name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
    }
}
