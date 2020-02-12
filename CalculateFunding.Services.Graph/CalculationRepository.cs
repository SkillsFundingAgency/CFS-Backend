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
        private IGraphRepository _graphRepository;

        public CalculationRepository(IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));

            _graphRepository = graphRepository;
        }

        public async Task DeleteCalculation(string calculationId)
        {
            await _graphRepository.DeleteNode<Calculation>("calculationid", calculationId);
        }

        public async Task SaveCalculations(IEnumerable<Calculation> calculations)
        {
            await _graphRepository.AddNodes(calculations.ToList(), new string[] { "calculationid" });
        }

        public async Task CreateCalculationRelationship(string calculationId, string specificationId)
        {
            await _graphRepository.CreateRelationship<Calculation, Specification>("BelongsToSpecification", ("calculationid", calculationId), ("specificationid", specificationId));
        }
    }
}
