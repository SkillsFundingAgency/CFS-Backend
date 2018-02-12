using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Proxies
{
    public interface IApiClientProxy
    {
        Task<T> GetAsync<T>(string url);
    }
}
