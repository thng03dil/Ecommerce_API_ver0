namespace Ecommerce.Application.Common.Pagination
{
    public class PagedResponse<T> 
    {
       
        public IEnumerable<T> Data { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; } 

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalCount)
        {

            Data = data; 
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize);
        }
    }
}
 