using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestEngine.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ICalculationsRepository
    {
        Task<byte[]> GetAssemblyBySpecificationId(string specificationId);
    }
}
