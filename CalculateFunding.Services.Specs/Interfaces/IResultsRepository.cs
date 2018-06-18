using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface IResultsRepository
    {
        Task<HttpStatusCode> PublishProviderResults(string specificationId);

        Task<bool> SpecificationHasResults(string specificationId);
    }
}
