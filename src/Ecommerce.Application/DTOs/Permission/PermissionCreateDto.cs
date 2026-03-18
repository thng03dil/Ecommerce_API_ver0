using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ecommerce.Application.DTOs.Permission
{
    public class PermissionCreateDto
    {

        [Required(ErrorMessage = "Entity is required")]
        [StringLength(50, ErrorMessage = "Entity cannot exceed 50 characters")]
        public string Entity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action is required")]
        [StringLength(50, ErrorMessage = "Action cannot exceed 50 characters")]
        public string Action { get; set; } = string.Empty;


        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
    }
}
