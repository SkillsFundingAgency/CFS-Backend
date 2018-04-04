using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task SaveProviderResults(IEnumerable<ProviderResult> providerResults);
    }
}
