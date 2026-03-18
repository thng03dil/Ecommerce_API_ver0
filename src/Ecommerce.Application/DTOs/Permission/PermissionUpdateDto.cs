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

        [Required(ErrorMessage = "Entity is required")]
        [StringLength(50, ErrorMessage = "Entity cannot exceed 50 characters")]
        public string Entity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action is required")]
        [StringLength(50, ErrorMessage = "Action cannot exceed 50 characters")]
        public string Action { get; set; } = string.Empty;

        public bool IsSystem { get; set; } = false;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
    }
}
