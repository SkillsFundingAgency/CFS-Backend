namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    internal class SpecificationMappingOption
    {
        public string SourceSpecificationId { get; set; }
        public string targetSpecificationId { get; set; }
        public string TargetRelationshipName { get; set; }
        public string TargetRelationshipDescription { get; set; }

        public string SourceRelationshipName { get; private set; }

        public string DetermineTargetRelationshipName(string relationshipName)
        {
            SourceRelationshipName = relationshipName;
            return string.IsNullOrEmpty(TargetRelationshipName) ? SourceRelationshipName : TargetRelationshipName;
        }

        public bool HasChangedRelationshipName => !string.IsNullOrEmpty(SourceRelationshipName) && SourceRelationshipName != TargetRelationshipName
                                                        &&!string.IsNullOrEmpty(TargetRelationshipName);

        public string FullyQualifiedSourceRelationshipName => string.IsNullOrEmpty(SourceRelationshipName) ? null : $"Datasets.{SourceRelationshipName}.";

        public string FullyQualifiedTargetRelationshipName => string.IsNullOrEmpty(TargetRelationshipName) ? null : $"Datasets.{TargetRelationshipName}.";
    }
}
