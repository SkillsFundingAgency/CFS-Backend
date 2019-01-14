using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class AllocationResult
    {
        public AllocationResult()
        {
            ProfilePeriods = new ProfilePeriod[0];
	        FinancialEnvelopes = Enumerable.Empty<FinancialEnvelope>();
			Calculations = Enumerable.Empty<CalculationResult>();
		}

        public AllocationLine AllocationLine { get; set; }

        public ushort AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public decimal AllocationAmount { get; set; }

        public ProfilePeriod[] ProfilePeriods { get; set; }

		public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }

        public int AllocationMajorVersion { get; set; }

        public int AllocationMinorVersion { get; set; }

	    public IEnumerable<CalculationResult> Calculations { get; set; }
    }
}