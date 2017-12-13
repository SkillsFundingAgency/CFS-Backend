using System.Net;

namespace CalculateFunding.ApiClient
{
    public class ApiResponse<T>
    {
        public ApiResponse(HttpStatusCode statusCode, T content = default(T))
        {
            StatusCode = statusCode;
            Content = content;
        }

        public HttpStatusCode StatusCode { get; private set; }
        public T Content { get; private set; }
    }
}
