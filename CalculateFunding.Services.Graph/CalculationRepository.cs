using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph
{
    public class CalculationRepository : ICalculationRepository
    {
        private const string CalculationId = "calculationid";
        private const string SpecificationId = "specificationid";
        private const string CalculationSpecificationRelationship = "BelongsToSpecification";
        private const string SpecificationCalculationRelationship = "HasCalculation";
        private const string CalculationACalculationBRelationship = "CallsCalculation";
        private const string CalculationBCalculationARelationship = "CalledByCalculation";

        private readonly IGraphRepository _graphRepository;

        public CalculationRepository(IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));

            _graphRepository = graphRepository;
        }

        public async Task DeleteCalculation(string calculationId)
        {
            await _graphRepository.DeleteNode<Calculation>(CalculationId, calculationId);
        }

        public async Task UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            await _graphRepository.UpsertNodes(calculations.ToList(), new string[] { CalculationId });
        }

        public async Task UpsertCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await _graphRepository.UpsertRelationship<Calculation, Specification>(CalculationSpecificationRelationship, 
                (CalculationId, calculationId), 
                (SpecificationId, specificationId));

            await _graphRepository.UpsertRelationship<Specification, Calculation>(SpecificationCalculationRelationship, 
                (SpecificationId, specificationId),
                (CalculationId, calculationId));
        }

        public async Task UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await _graphRepository.UpsertRelationship<Calculation, Calculation>(CalculationACalculationBRelationship, 
                (CalculationId, calculationIdA),
                (CalculationId, calculationIdB));

            await _graphRepository.UpsertRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationIdB),
                (CalculationId, calculationIdA));
        }

        public async Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await _graphRepository.DeleteRelationship<Calculation, Specification>(CalculationSpecificationRelationship, 
                (CalculationId, calculationId), 
                (SpecificationId, specificationId));

            await _graphRepository.DeleteRelationship<Specification, Calculation>(SpecificationCalculationRelationship,
                (SpecificationId, specificationId),
                (CalculationId, calculationId));
        }

        public async Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await _graphRepository.DeleteRelationship<Calculation, Calculation>(CalculationACalculationBRelationship, 
                (CalculationId, calculationIdA), 
                (CalculationId, calculationIdB));

            await _graphRepository.DeleteRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationIdB),
                (CalculationId, calculationIdA));
        }
    }
    
}
