using CalculateFunding.Models.Calcs;
using System.Collections.Generic;

namespace CalculateFunding.Services.Compiler.Interfaces
{
    public interface ICompilerFactory
    {
        ICompiler GetCompiler(IEnumerable<SourceFile> sourceFiles);
    }
}
