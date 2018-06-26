using System.Collections.Generic;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        Build GenerateCode(List<SourceFile> sourcefiles);
    }
}
