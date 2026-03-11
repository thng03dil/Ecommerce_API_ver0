namespace Ecommerce_API.Helpers.Pagination
{
    public class PagedResponse<T>
    {
        public bool Success { get; set; } = true;

        public string Message { get; set; } = "Get data successfully";
        public IEnumerable<T> Data { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalCount)
        {
        
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling((decimal)totalCount/(decimal)pageSize);
        }
    }
}
