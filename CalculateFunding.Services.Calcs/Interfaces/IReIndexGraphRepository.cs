using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IReIndexGraphRepository
    {
        Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships);
    }
}