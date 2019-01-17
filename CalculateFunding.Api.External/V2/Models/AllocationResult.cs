using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class AllocationResult
    {
        public AllocationResult()
        {
	        ProfilePeriods = new Collection<ProfilePeriod>();
	        FinancialEnvelopes = new Collection<FinancialEnvelope>();
			Calculations = new Collection<CalculationResult>();
		}

        public AllocationLine AllocationLine { get; set; }

        public ushort AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public decimal AllocationAmount { get; set; }

        public Collection<ProfilePeriod> ProfilePeriods { get; set; }

		public Collection<FinancialEnvelope> FinancialEnvelopes { get; set; }
		
        public int AllocationMajorVersion { get; set; }

        public int AllocationMinorVersion { get; set; }

	    public Collection<CalculationResult> Calculations { get; set; }
    }
}