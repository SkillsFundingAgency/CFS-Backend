using CsvHelper.Configuration;

namespace CalculateFunding.Generators.NavFeed.Providers.v2
{
    public class ProviderMap : ClassMap<Provider>
    {
        public ProviderMap()
        {
            Map(m => m.ID);
            Map(m => m.Application);
            Map(m => m.Interface);
            Map(m => m.DateTimeCreated).Name("DateTime Created");
            Map(m => m.Preprocessed);
            Map(m => m.Processed);
            Map(m => m.ErrorText).Name("Error Text");
            Map(m => m.NavisionReference).Name("Navision Reference");
            Map(m => m.ErrorLine).Name("Error Line");
            Map(m => m.DetailLines).Name("Detail Lines");
            Map(m => m.UnprocessedLines).Name("Unprocessed Lines ");
            Map(m => m.Boolean04).Name("Boolean04 ");
            Map(m => m.StartDay).Name("Start Day ");
            Map(m => m.StartMonth).Name("Start Month ");
            Map(m => m.EndDay).Name("End Day ");
            Map(m => m.EndMonth).Name("End Month ");
            Map(m => m.StartYear).Name("Start Year ");
            Map(m => m.EndYear).Name("End Year ");
            Map(m => m.MajorVersionNo).Name("Major Version No. ");
            Map(m => m.MinorVersionNo).Name("Minor Version No. ");
            Map(m => m.APIKey).Name("API Key ");
            Map(m => m.FundingStreamID).Name("Funding Stream ID ");
            Map(m => m.FundingStreamName).Name("Funding Stream Name ");
            Map(m => m.FundingShortName).Name("Funding Short Name ");
            Map(m => m.PeriodID).Name("Period ID ");
            Map(m => m.ProviderName).Name("Provider Name ");
            Map(m => m.ProviderLegalName).Name("Provider Legal Name ");
            Map(m => m.UKPRN).Name("UKPRN ");
            Map(m => m.UPIN).Name("UPIN ");
            Map(m => m.URN).Name("URN ");
            Map(m => m.DFEEstablishNo).Name("DFE Establish No. ");
            Map(m => m.EstablishmentNo).Name("Establishment No. ");
            Map(m => m.LACode).Name("LA Code ");
            Map(m => m.LocalAuthority).Name("Local Authority ");
            Map(m => m.Type).Name("Type ");
            Map(m => m.SubType).Name("Sub Type ");
            Map(m => m.NAVVendorNo).Name("NAV Vendor No. ");
            Map(m => m.ProviderStatus).Name("Status ");
            Map(m => m.AllocationID).Name("Allocation ID ");
            Map(m => m.AllocationName).Name("Allocation Name ");
            Map(m => m.AllocationShortName).Name("Allocation Short Name ");
            Map(m => m.FundingRoute).Name("Funding Route ");
            Map(m => m.ContractRequired).Name("Contract Required ");
            Map(m => m.AllocationStatus).Name("Allocation Status ");
            Map(m => m.PeriodTypeID).Name("Period Type ID ");
            Map(m => m.PeriodTypeName).Name("Period Type Name ");
            Map(m => m.NAVVendorName).Name("NAV Vendor Name ");
            Map(m => m.Successors).Name("Successors ");
            Map(m => m.Precessors).Name("Predecessors");
            Map(m => m.OpenReason).Name("Open Reason ");
            Map(m => m.CloseReason).Name("Close Reason ");
            Map(m => m.VariationReasons).Name("Variation Reasons ");
            Map(m => m.AllocationAmount).Name("Allocation Amount ");
            Map(m => m.OctoberProfileValue).Name("October Profile Amount");
            Map(m => m.AprilProfileValue).Name("April Profile Amount");
        }
    }
}
