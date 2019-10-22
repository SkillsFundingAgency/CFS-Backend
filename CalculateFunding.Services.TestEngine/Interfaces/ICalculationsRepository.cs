using System.Threading.Tasks;

namespace CalculateFunding.Services.TestEngine.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<byte[]> GetAssemblyBySpecificationId(string specificationId);
    }
}
