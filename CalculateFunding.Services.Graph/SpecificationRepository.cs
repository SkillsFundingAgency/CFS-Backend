using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph
{
    public class SpecificationRepository : ISpecificationRepository
    {
        private const string SpecificationId = "specificationid";
        
        private readonly IGraphRepository _graphRepository;

        public SpecificationRepository(IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));

            _graphRepository = graphRepository;
        }

        public async Task DeleteSpecification(string specificationId)
        {
            await _graphRepository.DeleteNode<Specification>(SpecificationId, specificationId);
        }

        public async Task UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            await _graphRepository.UpsertNodes(specifications.ToList(), new[] {SpecificationId});
        }

        public async Task DeleteAllForSpecification(string specificationId)
        {
            await _graphRepository.DeleteNodeAndChildNodes<Specification>(SpecificationId, specificationId);
        }
    }
}
