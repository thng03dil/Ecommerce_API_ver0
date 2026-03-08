using System.ComponentModel.DataAnnotations;

namespace Ecommerce_API.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } = string.Empty;

        public string Slug { get; set; }

        public ICollection<Product> Products { get; set; }
    }   
}
