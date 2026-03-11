namespace Ecommerce_API.DTOs.ProductDtos
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
    }
}
