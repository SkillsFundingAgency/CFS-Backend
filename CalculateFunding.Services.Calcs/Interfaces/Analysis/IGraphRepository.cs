using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Calcs.Interfaces.Analysis
{
    public interface IGraphRepository
    {
        Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships);
    }
}