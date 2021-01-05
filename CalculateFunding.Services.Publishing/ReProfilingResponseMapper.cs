using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing
{
    public class ReProfilingResponseMapper : IReProfilingResponseMapper
    {
        public IEnumerable<DistributionPeriod> MapReProfileResponseIntoDistributionPeriods(ReProfileResponse reProfileResponse) =>
            reProfileResponse.DeliveryProfilePeriods.GroupBy(_ => _.DistributionPeriod)
                .Select(_ => new DistributionPeriod
                {
                    Value = _.Sum(profilePeriod => profilePeriod.ProfileValue),
                    DistributionPeriodId = _.Key,
                    ProfilePeriods = _.Select(profilePeriod => new ProfilePeriod
                    {
                        DistributionPeriodId = _.Key,
                        Occurrence = profilePeriod.Occurrence,
                        Type = profilePeriod.Type.AsMatchingEnum<ProfilePeriodType>(),
                        Year = profilePeriod.Year,
                        ProfiledValue = profilePeriod.ProfileValue,
                        TypeValue = profilePeriod.TypeValue
                    })
                });
    }
}