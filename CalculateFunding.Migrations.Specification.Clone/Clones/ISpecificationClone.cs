using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    internal interface ISpecificationClone
    {
        Task Run(CloneOptions cloneOptions);
    }
}
