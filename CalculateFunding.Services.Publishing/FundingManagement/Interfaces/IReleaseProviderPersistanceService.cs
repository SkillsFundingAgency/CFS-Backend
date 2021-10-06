using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseProviderPersistanceService
    {
        Task<IEnumerable<ReleasedProvider>> ReleaseProviders(IEnumerable<string> providers, string specificationId);
    }
}
