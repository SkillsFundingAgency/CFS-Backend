using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using CalculateFunding.Common.Graph;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public abstract class GraphRepositoryTestBase
    {
        protected IGraphRepository GraphRepository;

        [TestInitialize]
        public void GraphRepositoryTestBaseSetUp()
        {
            GraphRepository = Substitute.For<IGraphRepository>();
        }

        protected Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        protected Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);

            return specificationBuilder.Build();
        }

        protected async Task ThenTheNodeWasDeleted<TNode>(string field, string value)
        {
            await GraphRepository
                .Received(1)
                .DeleteNode<TNode>(Arg.Is<Field>(_ => _.Name == field && _.Value == value));
        }
        
        protected string NewRandomString() => new RandomString();

        protected async Task ThenTheRelationshipWasCreated<TNodeA, TNodeB>(string label, 
            (string Name, string Value) left, 
            (string Name, string Value) right)
        {
            await GraphRepository
                .Received(1)
                .UpsertRelationship<TNodeA, TNodeB>(label,
                    Arg.Is<Field>(_ => _.Name == left.Name && _.Value == left.Value),
                    Arg.Is<Field>(_ => _.Name == right.Name && _.Value == right.Value));    
        }

        protected async Task AndTheRelationshipWasCreated<TNodeA, TNodeB>(string label,
            (string, string) left,
            (string, string) right)
        {
            await ThenTheRelationshipWasCreated<TNodeA, TNodeB>(label,
                left,
                right);
        }

        protected async Task ThenTheRelationshipWasDeleted<TNodeA, TNodeB>(string label, 
            (string Name, string Value) left, 
            (string Name, string Value) right)
        {
            await GraphRepository
                .Received(1)
                .DeleteRelationship<TNodeA, TNodeB>(label,
                    Arg.Is<Field>(_ => _.Name == left.Name && _.Value == left.Value),
                    Arg.Is<Field>(_ => _.Name == right.Name && _.Value == right.Value ));    
        }

        protected async Task AndTheRelationshipWasDeleted<TNodeA, TNodeB>(string label,
            (string, string) left,
            (string, string) right)
        {
            await ThenTheRelationshipWasDeleted<TNodeA, TNodeB>(label,
                left,
                right);
        }

        protected async Task ThenTheNodesWereCreated<TNode>(IEnumerable<TNode> nodes, params string[] indices)
        {
            await GraphRepository
                .Received(1)
                .UpsertNodes(Arg.Is<IEnumerable<TNode>>(_ => _.SequenceEqual<TNode>(nodes)),
                    Arg.Is<string[]>(_ => _.SequenceEqual(indices)));   
        }

        protected async Task ThenTheNodeWasCreated<TNode>(TNode node, params string[] indices)
        {
            await ThenTheNodesWereCreated(new[] {node}, indices);
        }

        protected async Task ThenTheNodeAndAllItsChildrenWereDeleted<TNode>(string field, string value)
        {
            await GraphRepository
                .Received(1)
                .DeleteNodeAndChildNodes<TNode>(Arg.Is<Field>(_ => _.Name == field && _.Value == value));
        }
    }
}
