using System.ComponentModel.DataAnnotations;

namespace Ecommerce_API.DTOs.ProductDtos
{
    public class ProductUpdateDto
    {
        public int? CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int Stock { get; set; }

        public bool IsActive { get; set; }
    }
}
