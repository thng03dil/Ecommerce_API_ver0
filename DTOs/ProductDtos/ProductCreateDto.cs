using System.ComponentModel.DataAnnotations;

namespace Ecommerce_API.DTOs.ProductDtos
{
    public class ProductCreateDto
    {
        public int? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }
    }
}
