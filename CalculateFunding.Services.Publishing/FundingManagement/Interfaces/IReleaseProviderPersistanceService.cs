using CalculateFunding.Common.Sql.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseProviderPersistanceService
    {
        Task ReleaseProviders(IEnumerable<string> providers, string specificationId);
    }
}
