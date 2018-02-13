namespace CalculateFunding.Services.Core.Proxies
{
    using CalculateFunding.Services.Core.Helpers;
    using CalculateFunding.Services.Core.Interfaces.Proxies;
    using Serilog;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpClientProxy : IHttpClient
    {
        private readonly HttpMessageHandler _handler;
        private readonly bool _disposeHandler;

        private HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpClientProxy(ILogger logger)
            : this(
                new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                },
                true)
        {
            _logger = logger;
        }

        public HttpClientProxy(HttpMessageHandler handler)
            : this(handler, true)
        {
        }

        public HttpClientProxy(HttpMessageHandler handler, bool disposeHandler)
        {
            _handler = handler;
            _disposeHandler = disposeHandler;

            if (handler == null)
            {
                _httpClient = new HttpClient();
            }
            else
            {
                _httpClient = new HttpClient(handler, disposeHandler);
            }
        }

        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        public Uri BaseAddress
        {
            get { return _httpClient.BaseAddress; }
            set { _httpClient.BaseAddress = value; }
        }

        public TimeSpan Timeout
        {
            get { return _httpClient.Timeout; }
            set { _httpClient.Timeout = value; }
        }

        public long MaxResponseContentBufferSize
        {
            get { return _httpClient.MaxResponseContentBufferSize; }
            set { _httpClient.MaxResponseContentBufferSize = value; }
        }

        public void CancelPendingRequests()
        {
            _httpClient.CancelPendingRequests();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.DeleteAsync(requestUri, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.DeleteAsync(requestUri, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.GetAsync(requestUri, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.GetAsync(requestUri, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.GetAsync(requestUri, completionOption, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.GetAsync(requestUri, completionOption, cancellationToken), _logger);
        }

        public Task<byte[]> GetByteArrayAsync(string requestUri)
        {
            return _httpClient.GetByteArrayAsync(requestUri);
        }

        public Task<byte[]> GetByteArrayAsync(Uri requestUri)
        {
            return _httpClient.GetByteArrayAsync(requestUri);
        }

        public Task<Stream> GetStreamAsync(string requestUri)
        {
            return _httpClient.GetStreamAsync(requestUri);
        }

        public Task<Stream> GetStreamAsync(Uri requestUri)
        {
            return _httpClient.GetStreamAsync(requestUri);
        }

        public Task<string> GetStringAsync(string requestUri)
        {
            return _httpClient.GetStringAsync(requestUri);
        }

        public Task<string> GetStringAsync(Uri requestUri)
        {
            return _httpClient.GetStringAsync(requestUri);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.PostAsync(requestUri, content, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.PostAsync(requestUri, content, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.PutAsync(requestUri, content, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.PutAsync(requestUri, content, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.SendAsync(request, cancellationToken), _logger);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RetryAgent.DoRequestAsync(() => _httpClient.SendAsync(request, completionOption, cancellationToken), _logger);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }
    }
}
