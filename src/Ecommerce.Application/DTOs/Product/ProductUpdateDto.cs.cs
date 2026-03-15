using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ecommerce.Application.DTOs.ProductDtos
{
    public class ProductUpdateDto
    {
        [JsonIgnore]
        public int Id { get; set; }

        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        public int CategoryId { get; set; }
    }
} 
 