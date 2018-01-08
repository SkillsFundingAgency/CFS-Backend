using System.Collections.Generic;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.CodeGeneration
{
    public interface ISourceFileGenerator
    {
        List<SourceFile> GenerateCode(Implementation implementation);
    }
}