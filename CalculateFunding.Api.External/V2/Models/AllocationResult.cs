using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class AllocationResult
    {
        public AllocationResult()
        {
            ProfilePeriods = new ProfilePeriod[0];
        }

        public AllocationLine AllocationLine { get; set; }

        public ushort AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public decimal AllocationAmount { get; set; }

        public ProfilePeriod[] ProfilePeriods { get; set; }

        public int AllocationMajorVersion { get; set; }

        public int AllocationMinorVersion { get; set; }
    }
}