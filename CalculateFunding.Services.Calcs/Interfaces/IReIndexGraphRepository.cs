using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IReIndexGraphRepository
    {
        Task<SpecificationCalculationRelationships> GetUnusedRelationships(SpecificationCalculationRelationships specificationCalculationRelationships);
        Task RecreateGraph(SpecificationCalculationRelationships specificationCalculationRelationships, SpecificationCalculationRelationships specificationCalculationUnusedRelationships);
    }
}