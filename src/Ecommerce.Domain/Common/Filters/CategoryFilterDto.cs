namespace Ecommerce.Domain.Common.Filters
{
    public class CategoryFilterDto
    {
        public string? Keyword { get; set; }

        public string? Slug { get; set; }


        public string? SortBy { get; set; } 

        public string? SortOrder { get; set; } 
    }
}
  