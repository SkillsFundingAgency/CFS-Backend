namespace Allocations.Services.DataImporter
{
    public class AptLocalAuthoritySourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Provider Information.Provider Name_9070")]
        public string ProviderName { get; set; }
        [SourceColumn("APT Proforma dataset.Basic Entitlement Primary Notional SEN_86424")]
        public decimal PrimaryNotionalSEN { get; set; }
    }
}