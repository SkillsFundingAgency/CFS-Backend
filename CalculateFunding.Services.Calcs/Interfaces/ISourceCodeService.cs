using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISourceCodeService
    {
        Task SaveAssembly(BuildProject buildProject);

        Task<byte[]> GetAssembly(BuildProject buildProject, CompilerOptions compilerOptions);

        Build Compile(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions);

        Task<IEnumerable<TypeInformation>> GetTypeInformation(BuildProject buildProject, CompilerOptions compilerOptions);

        IDictionary<string, string> GetCalulationFunctions(IEnumerable<SourceFile> sourceFiles);

        Task SaveSourceFiles(IEnumerable<SourceFile> sourceFiles, string specificationId);
    }
}
