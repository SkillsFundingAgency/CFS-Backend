namespace CalculateFunding.Models.Specs
{
    public class AssignSpecificationProviderVersionModel
    {
        public AssignSpecificationProviderVersionModel(string specificationId, string providerVersionId)
        {
            SpecificationId = specificationId;
            ProviderVersionId = providerVersionId;
        }

        public string SpecificationId { get; }

        public string ProviderVersionId { get; }
    }
}
