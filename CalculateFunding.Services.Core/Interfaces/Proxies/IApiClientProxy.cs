using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Proxies
{
    public interface IApiClientProxy
    {
        Task<T> GetAsync<T>(string url);

        Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request);

        Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request);
    }
}
