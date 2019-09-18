using CsvHelper.Configuration;

namespace CalculateFunding.Generators.NavFeed.Providers.v1
{
    public class ProviderMap : ClassMap<Provider>
    {
        public ProviderMap()
        {
            Map(m => m.providerId);
            Map(m => m.ProviderLaCode);
            Map(m => m.ProviderName);
            Map(m => m.ProviderNavVendorNumber);
            Map(m => m.ProviderPhaseOfEducation);
            Map(m => m.ProviderProviderSubType);
            Map(m => m.ProviderProviderType);
            Map(m => m.ProviderStatus);
            Map(m => m.ProviderUpin);
            Map(m => m.ProviderUrn);
            Map(m => m.ProviderEstablishmentNumber);
            Map(m => m.ProviderDfeEstablishmentNumber);
            Map(m => m.ProviderDateOpened);
            Map(m => m.ProviderDateClosed);
            Map(m => m.ProviderCrmAccountId);
            Map(m => m.ProviderAuthority);
            Map(m => m.title);
            Map(m => m.summary);
            Map(m => m.AllocationLineId);
            Map(m => m.AllocationLineName);
            Map(m => m.AllocationLineContractRequired);
            Map(m => m.AllocationLineFundingRoute);
            Map(m => m.AllocationLineResultAuthorName);
            Map(m => m.AllocationLineResultDate);
            Map(m => m.AllocationLineResultStatus);
            Map(m => m.AllocationLineResultVersion);
            Map(m => m.AllocationLineShortName);
            Map(m => m.AllocationLineResultValue);
            Map(m => m.FundingPeriodId);
            Map(m => m.FundingPeriodName);
            Map(m => m.FundingStreamId);
            Map(m => m.FundingStreamName);
            Map(m => m.FundingStreamPeriodTypeEndDay);
            Map(m => m.FundingStreamPeriodTypeEndMoth);
            Map(m => m.FundingStreamPeriodTypeId);
            Map(m => m.FundingStreamPeriodTypeName);
            Map(m => m.FundingStreamPeriodTypeStartDay);
            Map(m => m.FundingStreamPeriodTypeStartMonth);
            Map(m => m.FundingStreamShortName);
            Map(m => m.OctoberDistributionPeriod);
            Map(m => m.OctoberOccurrence);
            Map(m => m.OctoberPeriod);
            Map(m => m.OctoberPeriodType);
            Map(m => m.OctoberPeriodYear);
            Map(m => m.OctoberProfileValue);
            Map(m => m.AprilDistributionPeriod);
            Map(m => m.AprilOccurrence);
            Map(m => m.AprilPeriod);
            Map(m => m.AprilPeriodType);
            Map(m => m.AprilPeriodYear);
            Map(m => m.AprilProfileValue);
            Map(m => m.PupilCount);
        }
    }
}
