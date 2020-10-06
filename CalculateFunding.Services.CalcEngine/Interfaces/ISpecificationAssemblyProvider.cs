using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ISpecificationAssemblyProvider
    {
        Task<Stream> GetAssembly(string specificationId,
            string etag);

        Task SetAssembly(string specificationId, Stream assembly);
    }
}