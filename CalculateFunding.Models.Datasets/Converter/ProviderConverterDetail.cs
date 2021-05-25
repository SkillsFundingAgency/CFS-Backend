using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets.Converter
{
    public class ProviderConverterDetail : ProviderConverter
    {
        public string TargetProviderName { get; set; }

        public string TargetStatus { get; set; }

        public bool IsEligible => string.IsNullOrWhiteSpace(ProviderInEligible);

        public string ProviderInEligible { get; set; }

        public DateTimeOffset? TargetOpeningDate { get; set; }
    }
}
