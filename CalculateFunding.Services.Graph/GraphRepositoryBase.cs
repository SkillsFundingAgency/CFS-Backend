using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Graph
{
    public class GraphRepositoryBase
    {
        private readonly IGraphRepository _graphRepository;

        protected GraphRepositoryBase(IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));
            
            _graphRepository = graphRepository;
        }

        protected async Task DeleteNode<TNode>(string field, string value)
        {
            await _graphRepository.DeleteNode<TNode>(field, value);    
        }
        protected async Task UpsertNodes<TNode>(IEnumerable<TNode> nodes, params string[] indices)
        {
            await _graphRepository.UpsertNodes(nodes, indices);    
        }
        
        protected async Task UpsertNode<TNode>(TNode node, params string[] indices)
        {
            await _graphRepository.UpsertNodes(new [] { node }, indices);    
        }

        protected async Task DeleteNodeAndChildNodes<TNode>(string field, string value)
        {
            await _graphRepository.DeleteNodeAndChildNodes<TNode>(field, value);
        }

        protected async Task UpsertRelationship<TNodeA, TNodeB>(string label, (string, string) idA, (string, string) idB)
        {
            await _graphRepository.UpsertRelationship<TNodeA, TNodeB>(label, 
                (idA.Item1, idA.Item2), 
                (idB.Item1, idB.Item2));    
        }
        
        protected async Task DeleteRelationship<TNodeA, TNodeB>(string label, (string, string) idA, (string, string) idB)
        {
            await _graphRepository.DeleteRelationship<TNodeA, TNodeB>(label, 
                (idA.Item1, idA.Item2), 
                (idB.Item1, idB.Item2));    
        }
    }
}