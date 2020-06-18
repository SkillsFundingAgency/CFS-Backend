using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;
using Newtonsoft.Json.Linq;

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
            await _graphRepository.DeleteNode<TNode>(new Field { Name = field, Value = value });    
        }
        protected async Task UpsertNodes<TNode>(IEnumerable<TNode> nodes, params string[] indices)
        {
            await _graphRepository.UpsertNodes(nodes, indices);    
        }
        
        protected async Task UpsertNode<TNode>(TNode node, params string[] indices)
        {
            await _graphRepository.UpsertNodes(new [] { node }, indices);    
        }

        protected async Task UpsertRelationship<TNodeA, TNodeB>(string label, (string Name, string Value) idA, (string Name, string Value) idB)
        {
            await _graphRepository.UpsertRelationship<TNodeA, TNodeB>(label, 
                (new Field { Name = idA.Name, Value = idA.Value }), 
                (new Field { Name = idB.Name, Value = idB.Value }));    
        }

        protected async Task<IEnumerable<Entity<TNode>>> GetCircularDependencies<TNode> (string relationship, string field, string value)
            where TNode:class
        {
            return await _graphRepository.GetCircularDependencies<TNode>(relationship, new Field { Name = field, Value = value });
        }


        protected async Task DeleteRelationship<TNodeA, TNodeB>(string label, (string Name, string Value) idA, (string Name, string Value) idB)
        {
            await _graphRepository.DeleteRelationship<TNodeA, TNodeB>(label,
                (new Field { Name = idA.Name, Value = idA.Value }),
                (new Field { Name = idB.Name, Value = idB.Value }));
        }

        protected async Task<IEnumerable<Entity<TNode>>> GetAllEntities<TNode>(string field, string nodeId, IEnumerable<string> relationships)
            where TNode:class
        {
            return await _graphRepository.GetAllEntities<TNode>(new Field { Name = field, Value = nodeId }, relationships);
        }
    }
}