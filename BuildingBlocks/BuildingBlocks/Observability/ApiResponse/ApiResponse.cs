

namespace BuildingBlocks.Observability.ApiResponse
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "", int statusCode = 200)
            => new ApiResponse<T> { Success = true, Data = data, Message = message };


        public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
            => new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? new() };
    }
}
