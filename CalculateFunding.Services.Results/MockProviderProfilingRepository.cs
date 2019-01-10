using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Results
{
    public class MockProviderProfilingRepository : IProfilingApiClient
    {
        public Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            Guard.ArgumentNotNull(requestModel, nameof(requestModel));
            Guard.IsNullOrWhiteSpace(requestModel.FundingStreamPeriod, nameof(requestModel.FundingStreamPeriod));

            if (requestModel.AllocationValueByDistributionPeriod.IsNullOrEmpty())
            {
                throw new ArgumentException("Null or empty allocation distribution period found", nameof(requestModel.AllocationValueByDistributionPeriod));
            }

            ProviderProfilingResponseModel providerProfilingResponseModel = new ProviderProfilingResponseModel
            {
                AllocationProfileRequest = requestModel,
                DeliveryProfilePeriods = Enumerable.Empty<ProfilingPeriod>()

            };

            int startYear;
            int endYear;

            if (requestModel.FundingStreamPeriod.Length < 4)
            {
                startYear = DateTime.Now.Year;
                endYear = DateTime.Now.Year + 1;
            }
            else
            {
                string yearsPart = requestModel.FundingStreamPeriod.Substring(requestModel.FundingStreamPeriod.Length - 4);

                if (!int.TryParse(yearsPart, out var yearsPartAsInteger))
                {
                    startYear = DateTime.Now.Year;
                    endYear = DateTime.Now.Year + 1;
                }
                else
                {
                    startYear = int.Parse("20" + yearsPart.Substring(0, 2));
                    endYear = int.Parse("20" + yearsPart.Substring(2, 2));
                }
            }


            AllocationPeriodValue periodValue = requestModel.AllocationValueByDistributionPeriod.First();


            providerProfilingResponseModel.DeliveryProfilePeriods = new[]
            {
                new ProfilingPeriod
                {
                    DistributionPeriod = periodValue.DistributionPeriod,
                    Occurrence = 1,
                    Period = "Oct",
                    Type = "CalendarMonth",
                    Value = CalculateValue(periodValue.AllocationValue, 7),
                    Year = startYear
                },
                new ProfilingPeriod
                {
                    DistributionPeriod = periodValue.DistributionPeriod,
                    Occurrence = 1,
                    Period = "Apr",
                    Type = "CalendarMonth",
                    Value = CalculateValue(periodValue.AllocationValue, 5),
                    Year = endYear,
                }
            };

            return Task.FromResult(new ValidatedApiResponse<ProviderProfilingResponseModel>(System.Net.HttpStatusCode.OK, providerProfilingResponseModel));
        }


        decimal CalculateValue(decimal allocationValue, int monthlyMultiplier)
        {
            if (allocationValue == 0)
            {
                return 0;
            }

            decimal monthlyValue = allocationValue / 12;

            return decimal.Round((monthlyValue * monthlyMultiplier), 2, MidpointRounding.AwayFromZero);
        }
    }
}
