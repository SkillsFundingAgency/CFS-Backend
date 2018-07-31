using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{

    public class MockProviderProfilingRepository : IProviderProfilingRepository
    {
        public Task<ProviderProfilingResponseModel> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            Guard.ArgumentNotNull(requestModel, nameof(requestModel));

            decimal periodValue = requestModel.AllocationValuesByDistributionPeriod.First().Value == 0 ? 0 : (requestModel.AllocationValuesByDistributionPeriod.First().Value / 2);

            ProviderProfilingResponseModel model = new ProviderProfilingResponseModel
            {
                AllocationOrganisation = requestModel.AllocationOrganisation,
                AllocationvaluesByDistributionPeriod = requestModel.AllocationValuesByDistributionPeriod,
                FundingStreamPeriod = requestModel.FundingStreamPeriod,
                ProfilePeriods = new[]
                {
                    new ProfilingPeriod
                    {
                         DistributionPeriod = $"{requestModel.AllocationStartDate.Year}-{requestModel.AllocationEndDate.Year}",
                         Period = "Oct",
                         Occurrence = 1,
                         Year = requestModel.AllocationStartDate.Year,
                         Type = "CalendarMonth",
                         Value = periodValue,
                    },
                    new ProfilingPeriod
                    {
                         DistributionPeriod = $"{requestModel.AllocationStartDate.Year}-{requestModel.AllocationEndDate.Year}",
                         Period = "Apr",
                         Occurrence = 1,
                         Year = requestModel.AllocationEndDate.Year,
                         Type = "CalendarMonth",
                         Value = periodValue,
                    }
                }
            };

            return Task.FromResult(model);
        }
    }
}
