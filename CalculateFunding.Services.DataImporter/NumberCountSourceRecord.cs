namespace Allocations.Services.DataImporter
{
    public class NumberCountSourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Provider Information.Provider Name_9070")]
        public string ProviderName { get; set; }
        [SourceColumn("Census Number Counts.NOR_70999")]
        public int NumberOnRoll { get; set; }
        [SourceColumn("Census Number Counts.NOR Primary_71001")]
        public int NumberOnRollPrimary { get; set; }


    }
}