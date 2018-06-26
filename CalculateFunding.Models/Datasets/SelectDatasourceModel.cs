using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class SelectDatasourceModel
    {
        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public string DefinitionId { get; set; }

        public string DefinitionName{ get; set; }

        public string RelationshipId { get; set; }

        public string RelationshipName { get; set; }

        public IEnumerable<DatasetVersions> Datasets { get; set; }
    }
}
