namespace CalculateFunding.Services.Core.Interfaces.Proxies
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHttpClient : IDisposable
    {
        HttpRequestHeaders DefaultRequestHeaders { get; }

        Uri BaseAddress { get; set; }

        TimeSpan Timeout { get; set; }

        long MaxResponseContentBufferSize { get; set; }

        void CancelPendingRequests();

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken));

        Task<byte[]> GetByteArrayAsync(string requestUri);

        Task<byte[]> GetByteArrayAsync(Uri requestUri);

        Task<Stream> GetStreamAsync(string requestUri);

        Task<Stream> GetStreamAsync(Uri requestUri);

        Task<string> GetStringAsync(string requestUri);

        Task<string> GetStringAsync(Uri requestUri);

        Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken));
    }
}
