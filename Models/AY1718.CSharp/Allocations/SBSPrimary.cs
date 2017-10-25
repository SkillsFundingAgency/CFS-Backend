using System;
using Allocations.Models.Framework;
using AY1718.CSharp.Datasets;

namespace AY1718.CSharp.Allocations
{

    [Allocation("SBS1718")]

    public class SBSPrimary 
    {
        private static readonly DateTimeOffset April2018CutOff = new DateTimeOffset(2018, 4, 1, 0, 0, 0, TimeSpan.Zero);
        public AptProviderInformation AptProviderInformation { get; set; }
        public AptBasicEntitlement AptBasicEntitlement { get; set; }
        public CensusNumberCounts CensusNumberCounts { get; set; }

        public CalculationResult P004_PriRate()
        {
            return new CalculationResult("P004_PriRate", AptBasicEntitlement.PrimaryAmountPerPupil);
        }

        public CalculationResult P005_PriBESubtotal()
        {
            if (AptProviderInformation.DateOpened > April2018CutOff)
            {
                return new CalculationResult("P005_PriBESubtotal", AptBasicEntitlement.PrimaryAmount);
            }

            return new CalculationResult("P005_PriBESubtotal", P004_PriRate().Value * CensusNumberCounts.NORPrimary);
        }

        public CalculationResult P006a_NSEN_PriBE_Percent()
        {
            return new CalculationResult("P006a_NSEN_PriBE_Percent", AptBasicEntitlement.PrimaryNotionalSEN);
        }

        public CalculationResult P006_NSEN_PriBE()
        {
            return new CalculationResult("P006_NSEN_PriBE", P006a_NSEN_PriBE_Percent().Value * P005_PriBESubtotal().Value);
        }

    }
}