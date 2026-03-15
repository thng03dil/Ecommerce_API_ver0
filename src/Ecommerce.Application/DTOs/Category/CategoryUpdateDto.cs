using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ecommerce.Application.DTOs.CategoryDtos
{
    public class CategoryUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Slug is required")]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty; 


    }
}
 