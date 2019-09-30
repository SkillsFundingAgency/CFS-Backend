using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class CurrentSpecificationStepContext : ICurrentSpecificationStepContext
    {
        public string SpecificationId { get; set; }

        public SpecificationInMemoryRepository Repo { get; set; }
    }
}
