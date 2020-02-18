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
    public class SpecificationRepository : ISpecificationRepository
    {
        private IGraphRepository _graphRepository;

        public SpecificationRepository(IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));

            _graphRepository = graphRepository;
        }

        public async Task DeleteSpecification(string specificationid)
        {
            await _graphRepository.DeleteNode<Specification>("specificationid", specificationid);
        }

        public async Task UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            await _graphRepository.UpsertNodes(specifications.ToList(), new string[] { "specificationid" });
        }
    }
}
