using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);

        Task<IEnumerable<FundingStream>> GetFundingStreams();
    }
}
