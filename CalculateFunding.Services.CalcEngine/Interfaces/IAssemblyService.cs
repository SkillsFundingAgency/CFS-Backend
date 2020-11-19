using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAssemblyService
    {
        Task<byte[]> GetAssemblyForSpecification(string specificationId, string etag);
    }
}
