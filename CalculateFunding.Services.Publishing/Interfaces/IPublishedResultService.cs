using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface  IPublishedResultService
    {
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId);
    }
}
