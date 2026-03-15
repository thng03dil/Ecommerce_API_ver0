using Ecommerce.Application.Common.Pagination;

namespace Ecommerce.Application.Common.Responses
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; } = false;

        public string Message { get; set; } 

        public T? Data { get; set; }
        public ApiResponse(int statusCode , bool success, string message, T data )
        {
            StatusCode = statusCode;
            Success = success;
            Message = message;
            Data = data;
           }
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>(200, true, message, data);
        } 
    }
}
 