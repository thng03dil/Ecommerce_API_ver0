namespace Ecommerce_API.DTOs.Common
{
    public class PaginationDto
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
