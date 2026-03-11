using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ecommerce_API.DTOs.CategoryDtos
{
    public class CategoryUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Slug { get; set; } = string.Empty;


    }
}
