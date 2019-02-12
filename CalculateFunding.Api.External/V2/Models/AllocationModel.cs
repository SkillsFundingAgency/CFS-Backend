using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V2.Models
{
    /// <summary>
    /// Represents an allocation model
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class AllocationModel
    {
        public AllocationModel() { }

        public AllocationModel(AllocationFundingStreamModel fundingStream, Period period, AllocationProviderModel provider, AllocationLine allocationLine,
            string status, decimal allocationAmount, string allocationResultId)
        {
            FundingStream = fundingStream;
            Period = period;
            Provider = provider;
            AllocationLine = allocationLine;
            AllocationStatus = status;
            AllocationAmount = allocationAmount;
            AllocationResultId = allocationResultId;
        }

        /// <summary>
        /// The allocation id of allocation model
        /// </summary>
        public string AllocationId
        {
            get
            {
                if (AllocationLine == null)
                {
                    throw new InvalidOperationException($"{nameof(AllocationLine)} is null");
                }

                if (string.IsNullOrWhiteSpace(AllocationLine.Id))
                {
                    throw new InvalidOperationException($"{nameof(AllocationLine)}.{nameof(AllocationLine.Id)} is null or empty");
                }

                if (Period == null)
                {
                    throw new InvalidOperationException($"{nameof(Period)} is null");
                }

                if (string.IsNullOrWhiteSpace(Period.Id))
                {
                    throw new InvalidOperationException($"{nameof(Period)}.{nameof(Period.Id)} is null or empty");
                }

                if (Provider == null)
                {
                    throw new InvalidOperationException($"{nameof(Provider)} is null");
                }

                if (string.IsNullOrWhiteSpace(Provider.ProviderId))
                {
                    throw new InvalidOperationException($"{nameof(Provider)}.{nameof(Provider.ProviderId)} is null or empty");
                }

                return $"{AllocationLine.Id}-{Period.Id}-{Provider.ProviderId}";
            }
        }

        /// <summary>
        /// The allocation result id of allocation model
        /// </summary>
        public string AllocationResultId { get; set; }

        /// <summary>
        /// The current allocation result title
        /// </summary>
        public string AllocationResultTitle { get; set; }

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
        /// The current allocation major version
        /// </summary>
        public int AllocationMajorVersion { get; set; }

        /// <summary>
        /// The current allocation minor version
        /// </summary>
        public int AllocationMinorVersion { get; set; }

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
        public IEnumerable<ProfilePeriod> ProfilePeriods { get; set; }
    }
}
