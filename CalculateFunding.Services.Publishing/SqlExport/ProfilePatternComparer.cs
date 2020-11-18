using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProfilePatternComparer : IEqualityComparer<ProfilePeriodPattern>
    {
        public bool Equals([AllowNull] ProfilePeriodPattern x, [AllowNull] ProfilePeriodPattern y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Occurrence == y.Occurrence
                   && x.Period == y.Period
                   && x.PeriodType == y.PeriodType
                   && x.PeriodYear == y.PeriodYear;
        }

        public int GetHashCode([DisallowNull] ProfilePeriodPattern obj)
        {
            return obj.GetHashCode();
        }
    }
}
