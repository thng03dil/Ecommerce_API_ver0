
using System.ComponentModel.DataAnnotations;
namespace Ecommerce.Application.DTOs.CategoryDtos
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Slug is required")]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;
    }
}
  