using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ecommerce_API.DTOs.ProductDtos
{
    public class ProductUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }
        public int? CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int Stock { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
