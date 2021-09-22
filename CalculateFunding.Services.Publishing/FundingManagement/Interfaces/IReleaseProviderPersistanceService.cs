using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseProviderPersistanceService
    {
        Task ReleaseProviders(IEnumerable<string> providers, string specificationId);
    }
}
