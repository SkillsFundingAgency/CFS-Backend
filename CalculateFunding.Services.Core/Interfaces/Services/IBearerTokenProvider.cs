using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IBearerTokenProvider
    {
        Task<string> GetToken();
    }
}
