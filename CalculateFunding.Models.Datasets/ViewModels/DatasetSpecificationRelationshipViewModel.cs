using System;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets.ViewModels
{
    public class DatasetSpecificationRelationshipViewModel : Reference
    {
        public DatasetDefinitionViewModel Definition { get; set; }

        public string DatasetName { get; set; }

        public int? Version { get; set; }

        public string DatasetId { get; set; }

        public bool ConverterEligible { get; set; }

        public bool ConverterEnabled { get; set; }

        public string RelationshipDescription { get; set; }

        public bool IsProviderData { get; set; }

        public bool IsLatestVersion { get; set; }
        
        public DateTimeOffset? LastUpdatedDate { get; set; }
        
        public Reference LastUpdatedAuthor { get; set; }
    }
}
