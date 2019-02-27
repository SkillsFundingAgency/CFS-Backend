using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISourceCodeService
    {
        Task SaveAssembly(BuildProject buildProject);

        Task<byte[]> GetAssembly(BuildProject buildProject);

        Build Compile(BuildProject buildProject, IEnumerable<Calculation> calculations);

        Task<IEnumerable<TypeInformation>> GetTypeInformation(BuildProject buildProject);

        IDictionary<string, string> GetCalulationFunctions(IEnumerable<SourceFile> sourceFiles);

        Task SaveSourceFiles(IEnumerable<SourceFile> sourceFiles, string specificationId);
    }
}
