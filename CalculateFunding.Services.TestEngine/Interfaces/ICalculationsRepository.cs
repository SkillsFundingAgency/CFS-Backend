using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestEngine.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<byte[]> GetAssemblyBySpecificationId(string specificationId);
    }
}
