using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class AllocationWithHistoryModel : AllocationModel
    {
        public AllocationWithHistoryModel()
        {

        }

        public AllocationWithHistoryModel(AllocationModel allocationModel) :
            base(allocationModel.FundingStream, allocationModel.Period, allocationModel.Provider, allocationModel.AllocationLine,
                allocationModel.AllocationStatus, allocationModel.AllocationAmount, allocationModel.AllocationResultId)
        {
            ProfilePeriods = allocationModel.ProfilePeriods;
        }

        public AllocationWithHistoryModel(AllocationFundingStreamModel fundingStream, Period period, AllocationProviderModel provider, AllocationLine allocationLine,
           int allocationVersionNumber, string status, decimal allocationAmount, int? allocationLearnerCount, string allocationResultId, List<ProfilePeriod> profilePeriods)
            : base(fundingStream, period, provider, allocationLine, status, allocationAmount, allocationResultId)
        {
            ProfilePeriods = profilePeriods;
        }

        public Collection<AllocationHistoryModel> History { get; set; }
    }
}
