using System.ComponentModel.DataAnnotations;

namespace Ecommerce_API.DTOs.CategoryDtos
{
    public class CategoryUpdateDto
    {
       
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
