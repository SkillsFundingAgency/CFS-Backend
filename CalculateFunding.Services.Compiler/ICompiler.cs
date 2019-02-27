using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        Build GenerateCode(List<SourceFile> sourcefiles);

        IDictionary<string, string> GetCalulationFunctions(IEnumerable<SourceFile> sourceFiles);
    }
}
