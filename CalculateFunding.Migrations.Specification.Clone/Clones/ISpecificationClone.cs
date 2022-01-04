using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    internal interface ISpecificationClone
    {
        Task<bool> ValidateConfiguration(CloneOptions cloneOptions);

        Task Run(CloneOptions cloneOptions);
    }
}
