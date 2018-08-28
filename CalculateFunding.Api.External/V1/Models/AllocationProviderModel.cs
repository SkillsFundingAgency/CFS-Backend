using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Models
{
	/// <summary>
	/// Fields are made available only if it is provided from the provider datasource
	/// </summary>
	[Serializable]
    public class AllocationProviderModel
    {
        public AllocationProviderModel()
        {
        }

        public AllocationProviderModel(string ukprn, string upin, DateTime? providerOpenDate)
        {
            Ukprn = ukprn;
            Upin = upin;
            ProviderOpenDate = providerOpenDate;
        }

        public string Ukprn { get; set; }

        public string Upin { get; set; }

        public DateTimeOffset? ProviderOpenDate { get; set; }
    }
}
