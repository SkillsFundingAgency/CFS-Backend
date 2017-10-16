using System;
using Academies.AY1718.Datasets;
using Allocations.Models.Framework;

namespace Academies.AY1718.Allocations
{

    [Allocation("SBS1718")]

    public class SBSPrimary 
    {
        private static readonly DateTimeOffset April2018CutOff = new DateTimeOffset(2018, 4, 1, 0, 0, 0, TimeSpan.Zero);
        public AptProviderInformation AptProviderInformation { get; set; }
        public AptBasicEntitlement AptBasicEntitlement { get; set; }
        public CensusNumberCounts CensusNumberCounts { get; set; }

        public ProviderAllocation P004_PriRate()
        {
            return new ProviderAllocation("P004_PriRate", AptBasicEntitlement.PrimaryAmountPerPupil);
        }

        public ProviderAllocation P005_PriBESubtotal()
        {
            if (AptProviderInformation.DateOpened > April2018CutOff)
            {
                return new ProviderAllocation("P005_PriBESubtotal", AptBasicEntitlement.PrimaryAmount);
            }

            return new ProviderAllocation("P005_PriBESubtotal", P004_PriRate().Value * CensusNumberCounts.NORPrimary);
        }

        public ProviderAllocation P006a_NSEN_PriBE_Percent()
        {
            return new ProviderAllocation("P004_PriRate", AptBasicEntitlement.PrimaryNotionalSEN);
        }

        public ProviderAllocation P006_NSEN_PriBE()
        {
            return new ProviderAllocation("P004_PriRate", P006a_NSEN_PriBE_Percent().Value * P005_PriBESubtotal().Value);
        }

    }
}