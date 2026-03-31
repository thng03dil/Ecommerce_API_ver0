

namespace Ecommerce.Application.Common.Responses
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; } = false;

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }
        public ApiResponse(int statusCode , bool success, string message, T? data = default )
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
        public static ApiResponse<T> SuccessResponse(int statusCode, T data, string message = "Success")
        {
            return new ApiResponse<T>(201, true, message, data);
        }
        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400)
        {
            // Bây giờ truyền default (null) vào đây sẽ không còn bị Warning
            return new ApiResponse<T>(statusCode, false, message, default);
        }
    }
}
 