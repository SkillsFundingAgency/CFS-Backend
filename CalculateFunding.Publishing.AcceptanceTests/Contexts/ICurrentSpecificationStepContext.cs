using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface ICurrentSpecificationStepContext
    {
        string SpecificationId { get; set;}

        SpecificationInMemoryRepository Repo { get; }
    }
}