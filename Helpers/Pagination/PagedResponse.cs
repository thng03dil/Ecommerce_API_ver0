namespace Ecommerce_API.Helpers.Pagination
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public PagedResponse(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling((decimal)totalCount/(decimal)pageSize);
        }
    }
}
