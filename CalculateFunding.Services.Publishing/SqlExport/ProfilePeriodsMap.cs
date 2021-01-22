using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProfilePeriodsMap
    {
        private readonly int[] _periodPatternHashes;

        private static readonly object[] NullPeriodColumns = new object[]
        {
            null,
            null,
            null,
            null,
            null,
            null
        };

        public ProfilePeriodsMap(ProfilePeriodPattern[] profilePeriodPatterns)
        {
            Guard.ArgumentNotNull(profilePeriodPatterns, nameof(profilePeriodPatterns));

            _periodPatternHashes = profilePeriodPatterns
                .Select(GetProfilePeriodPatternHash)
                .ToArray();
        }

        public IEnumerable<object[]> GetProfilePeriodValues(ProfileTotal[] profileTotals)
        {
            IDictionary<int, ProfileTotal> profileTotalsMap = profileTotals
                .ToDictionary(GetProfileTotalPatternHash);
            
            foreach (int patternHash in _periodPatternHashes)
            {
                if (profileTotalsMap.TryGetValue(patternHash, out ProfileTotal profileTotal))
                {
                    yield return GetProfilePeriodColumnsForProfileTotal(profileTotal);
                    
                    continue;
                }

                yield return NullPeriodColumns;
            }
        }
        
        private object[] GetProfilePeriodColumnsForProfileTotal(ProfileTotal profile)
            => new object[]
            {
                profile.TypeValue,
                profile.PeriodType,
                profile.Year,
                profile.Occurrence,
                profile.DistributionPeriod,
                profile.Value
            };
        
        private int GetProfileTotalPatternHash(ProfileTotal profileTotal)
            => HashCode.Combine(profileTotal?.Year,
                profileTotal?.TypeValue,
                profileTotal?.Occurrence);

        private int GetProfilePeriodPatternHash(ProfilePeriodPattern profilePeriodPattern)
            => HashCode.Combine(profilePeriodPattern?.PeriodYear,
                profilePeriodPattern?.Period,
                profilePeriodPattern?.Occurrence);
    }
}