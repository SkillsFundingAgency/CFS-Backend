using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
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
            await _graphRepository.DeleteNode<TNode>(NewField(field, value));    
        }

        protected async Task DeleteNodes<TNode>(params (string field, string value)[] fields)
        {
            await _graphRepository.DeleteNodes<TNode>(fields.Select(NewField).ToArray());
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
                NewField(idA), 
                NewField(idB));    
        }

        protected async Task UpsertRelationships<TNodeA, TNodeB>(params (string label, (string Name, string Value) idA, (string Name, string Value) idB)[] relationships)
        {
            await _graphRepository.UpsertRelationships<TNodeA, TNodeB>(ToAmendRelationshipRequests(relationships));
        }

        private IField NewField((string Name, string Value) field) => NewField(field.Name, field.Value);

        private IField NewField(string name,
            string value) => new Field
        {
            Name = name,
            Value = value
        };

        protected async Task<IEnumerable<Entity<TNode>>> GetCircularDependencies<TNode> (string relationship, string field, string value)
            where TNode:class
        {
            return await _graphRepository.GetCircularDependencies<TNode>(relationship, NewField(field, value));
        }

        protected async Task<IEnumerable<Entity<TNode>>> GetCircularDependencies<TNode>(string relationship, string field, string[] values)
            where TNode : class
        {
            return await _graphRepository.GetCircularDependencies<TNode>(relationship, values.Select(_ => NewField(field, _)));
        }

        protected async Task DeleteRelationship<TNodeA, TNodeB>(string label, (string Name, string Value) idA, (string Name, string Value) idB)
        {
            await _graphRepository.DeleteRelationship<TNodeA, TNodeB>(label,
                NewField(idA), 
                NewField(idB));
        }

        protected async Task DeleteRelationships<TNodeA, TNodeB>(params (string label, (string Name, string Value) idA, (string Name, string Value) idB)[] fields)
        {
            await _graphRepository.DeleteRelationships<TNodeA, TNodeB>(ToAmendRelationshipRequests(fields));
        }

        private AmendRelationshipRequest[] ToAmendRelationshipRequests((string label, (string Name, string Value) idA, (string Name, string Value) idB)[] relationships)
        {
            return relationships.Select(_ 
                    => new AmendRelationshipRequest
                    {
                        A = (Field) NewField(_.idA),
                        B = (Field) NewField(_.idB),
                        Type = _.label
                    })
                .ToArray();
        }

        protected async Task<IEnumerable<Entity<TNode>>> GetAllEntitiesForAll<TNode>(string field,
            IEnumerable<string> nodeIds,
            IEnumerable<string> relationships)
            where TNode : class
        {
            return await _graphRepository.GetAllEntitiesForAll<TNode>(nodeIds.Select(nodeId
                        => NewField(field, nodeId))
                    .ToArray(),
                relationships);
        }
        
        protected async Task<IEnumerable<Entity<TNode>>> GetAllEntities<TNode>(string field, string nodeId, IEnumerable<string> relationships)
            where TNode:class
        {
            return await _graphRepository.GetAllEntities<TNode>(NewField(field, nodeId), relationships);
        }
    }
}