using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISourceFileRepository
    {
        Task SaveSourceFiles(byte[] zippedContent, string specificationId, string sourceType);

        Task SaveAssembly(byte[] assemblyBytes, string specificationId);

        Task<Stream> GetAssembly(string specificationId);

        Task<bool> DoesAssemblyExist(string specificationId);

        Task<bool> DeleteAssembly(string specificationId);
        
        Task<string> GetAssemblyETag(string specificationId);
    }
}
