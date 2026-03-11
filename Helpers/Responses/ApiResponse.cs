namespace Ecommerce_API.Helpers.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } 

        public T Data { get; set; }
        public ApiResponse(bool success, string message,T items )
        {
            Success = success;
            Message = message;
            Data = items;
           }
    }
}
