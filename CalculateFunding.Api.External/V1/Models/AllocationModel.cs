using CalculateFunding.Models.External;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
{
    /// <summary>
    /// Represents an allocation model
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class AllocationModel
    {
        public AllocationModel(){}

        public AllocationModel(AllocationFundingStreamModel fundingStream, Period period, AllocationProviderModel provider, AllocationLine allocationLine,
           int allocationVersionNumber, string status, decimal allocationAmount, string allocationResultId)
        {
            FundingStream = fundingStream;
            Period = period;
            Provider = provider;
            AllocationLine = allocationLine;
            AllocationVersionNumber = allocationVersionNumber;
            AllocationStatus = status;
            AllocationAmount = allocationAmount;
            AllocationResultId = allocationResultId;
        }

        /// <summary>
        /// The allocation result id of allocation model
        /// </summary>
        public string AllocationResultId { get; set; }

        /// <summary>
        /// The funding stream associated with the allocation model
        /// </summary>
        public AllocationFundingStreamModel FundingStream { get; set; }

        /// <summary>
        /// The period associated with the allocation model
        /// </summary>
        public Period Period { get; set; }

        /// <summary>
        /// The provider associated with the allocation model
        /// </summary>
        public AllocationProviderModel Provider { get; set; }

        /// <summary>
        /// The allocation line associated with the allocation model
        /// </summary>
        public AllocationLine AllocationLine { get; set; }

        /// <summary>
        /// The current allocation version number
        /// </summary>
        public int AllocationVersionNumber { get; set; }

        /// <summary>
        /// The current allocation status
        /// </summary>
        public string AllocationStatus { get; set; }

        /// <summary>
        /// The current allocation amount
        /// </summary>
        public decimal AllocationAmount { get; set; }

        /// <summary>
        /// The profiling periods associated allocation model
        /// </summary>
        public ProfilePeriod[] ProfilePeriods { get; set; }
    }
}
