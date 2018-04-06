using System.Collections.Generic;
using CalculateFunding.Models.Code;

namespace CalculateFunding.Services.CodeMetadataGenerator.Interfaces
{
    public interface ICodeMetadataGeneratorService
    {
        IEnumerable<TypeInformation> GetTypeInformation(byte[] rawAssembly);
    }
}
