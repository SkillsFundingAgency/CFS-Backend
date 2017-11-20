using System;

namespace Allocations.Services.DataImporter
{
    public class AptSourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Provider Information.UPIN_9068")]
        public string UPIN { get; set; }
        [SourceColumn("Provider Information.Provider Name_9070")]
        public string ProviderName { get; set; }
        [SourceColumn("Provider Information.Date Opened_9077")]
        public DateTime DateOpened { get; set; }
        [SourceColumn("Provider Information.Local Authority_9426")]
        public string LocalAuthority { get; set; }

        [SourceColumn("APT Adjusted Factors dataset.Phase_71703")]
        public string Phase { get; set; }

        [SourceColumn("APT New ISB dataset.Basic Entitlement Primary_71855")]
        public decimal PrimaryAmount { get; set; }
        [SourceColumn("APT New ISB dataset.15-16 Post MFG per pupil Budget_71961")]
        public decimal PrimaryAmountPerPupil { get; set; }

        [SourceColumn("APT Inputs and Adjustments.NOR_71991")]
        public decimal NumberOnRoll { get; set; }

        [SourceColumn("APT Inputs and Adjustments.NOR Primary_71993")]
        public decimal NumberOnRollPrimary { get; set; }


    }
}