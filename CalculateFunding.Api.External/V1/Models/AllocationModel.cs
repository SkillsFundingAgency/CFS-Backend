using CalculateFunding.Models.External;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class AllocationModel
    {
        public AllocationModel()
        {

        }

        public AllocationModel(AllocationFundingStreamModel fundingStream, Period period, AllocationProviderModel provider, AllocationLine allocationLine,
           int allocationVersionNumber, string status, decimal allocationAmount, int? allocationLearnerCount, string allocationResultId)
        {
            FundingStream = fundingStream;
            Period = period;
            Provider = provider;
            AllocationLine = allocationLine;
            AllocationVersionNumber = allocationVersionNumber;
            AllocationStatus = status;
            AllocationAmount = allocationAmount;
            AllocationLearnerCount = allocationLearnerCount;
            AllocationResultId = allocationResultId;
        }

        public string AllocationResultId { get; set; }

        public AllocationFundingStreamModel FundingStream { get; set; }

        public Period Period { get; set; }

        public AllocationProviderModel Provider { get; set; }

        public AllocationLine AllocationLine { get; set; }

        public int AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public decimal AllocationAmount { get; set; }

        public int? AllocationLearnerCount { get; set; }

        public ProfilePeriod[] ProfilePeriods { get; set; }
    }
}
