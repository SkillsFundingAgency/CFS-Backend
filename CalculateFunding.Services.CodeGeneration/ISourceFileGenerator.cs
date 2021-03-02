using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;

namespace CalculateFunding.Services.CodeGeneration
{
    public interface ISourceFileGenerator
    {
        List<SourceFile> GenerateCode(BuildProject buildProject,
            IEnumerable<Calculation> calculations,
            CompilerOptions compilerOptions,
            IEnumerable<ObsoleteItem> obsoleteItems = null);
    }
}