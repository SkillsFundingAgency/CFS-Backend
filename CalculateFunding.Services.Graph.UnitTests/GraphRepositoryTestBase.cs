using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using CalculateFunding.Common.Graph;
using FluentAssertions;
using Serilog.Sinks.File;

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
        protected FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected async Task ThenTheNodeWasDeleted<TNode>(string field, string value)
        {
            await GraphRepository
                .Received(1)
                .DeleteNode<TNode>(Arg.Is<Field>(_ => _.Name == field && _.Value == value));
        }
        
        protected async Task ThenTheNodesWereDeleted<TNode>(params (string field, string value)[] nodes)
        {
            await GraphRepository
                .Received(1)
                .DeleteNodes<TNode>(Arg.Is<IField[]>(_ => FieldsMatch(_, AsFields(nodes))));
        }

        private Field[] AsFields(params (string field, string value)[] nodes) => nodes.Select(_ => new Field
        {
            Name = _.field,
            Value = _.value
        }).ToArray();
        
        private bool FieldsMatch(IField[] actual,
            Field[] expected)
        {
            actual
                .Should()
                .BeEquivalentTo<Field>(expected);

            return true;
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

        protected async Task AndTheRelationshipsWereCreated<TNodeA, TNodeB>(string label,
            params ((string, string), (string, string))[] relationships)
            => await ThenTheRelationshipsWereCreated<TNodeA, TNodeB>(label, relationships);
        
        protected async Task ThenTheRelationshipsWereCreated<TNodeA, TNodeB>(string label,
            params ((string, string), (string, string))[] relationships)
        {
            await GraphRepository
                .Received(1)
                .UpsertRelationships<TNodeA, TNodeB>(Arg.Is<AmendRelationshipRequest[]>(requests =>
                    TheRequestsMatch(requests, label, relationships)));
        }

        protected async Task AndTheRelationshipsWereDeleted<TNodeA, TNodeB>(string label,
            params ((string, string), (string, string))[] relationships)
            => await ThenTheRelationshipsWereDeleted<TNodeA, TNodeB>(label, relationships);
        
        protected async Task ThenTheRelationshipsWereDeleted<TNodeA, TNodeB>(string label,
            params ((string, string), (string, string))[] relationships)
        {
            await GraphRepository
                .Received(1)
                .DeleteRelationships<TNodeA, TNodeB>(Arg.Is<AmendRelationshipRequest[]>(requests =>
                    TheRequestsMatch(requests, label, relationships)));
        }

        private bool TheRequestsMatch(AmendRelationshipRequest[] actual,
            string label,
            params ((string Name, string Value) left,
                (string Name, string Value) right)[] expected)
        {
            actual
                .Should()
                .BeEquivalentTo<AmendRelationshipRequest>(
                    ToAmendRelationshipRequests(label, expected));

            return true;
        }
        
        private AmendRelationshipRequest[] ToAmendRelationshipRequests(string label, ((string, string) idA, (string, string) idB)[] relationships)
        {
            return relationships.Select(_ 
                    => new AmendRelationshipRequest
                    {
                        A = (Field) NewField(_.idA),
                        B = (Field) NewField(_.idB),
                        Type = label
                    })
                .ToArray();
        }
        
        private IField NewField((string Name, string Value) field) => NewField(field.Name, field.Value);
        
        private IField NewField(string name,
            string value) => new Field
        {
            Name = name,
            Value = value
        };

        protected async Task AndTheRelationshipWasCreated<TNodeA, TNodeB>(string label,
            (string, string) left,
            (string, string) right)
        {
            await ThenTheRelationshipWasCreated<TNodeA, TNodeB>(label,
                left,
                right);
        }

        protected void GivenCircularDependencies(string relationship, string fieldName, string fieldValue, Entity<Calculation> entity)
        {
            GraphRepository.GetCircularDependencies<Calculation>(relationship, Arg.Is<IField>(_ => _.Name == fieldName && _.Value == fieldValue))
                .Returns(new[] { entity });
        }

        protected void GivenTheEntities<T> (string[] relationships, string fieldName, string fieldValue, Entity<T> entity) where T:class
        {
            GraphRepository.GetAllEntities<T>(Arg.Is<IField>(_ => _.Name == fieldName && _.Value == fieldValue), Arg.Is<string[]>(_ => _.EqualTo(relationships)))
                .Returns(new[] { entity });
        }
        
        protected void GivenTheEntitiesForAll<T> (string[] relationships, (string fieldName, string fieldValue)[] fields, params Entity<T>[] entities) where T:class
        {
            GraphRepository.GetAllEntitiesForAll<T>(Arg.Is<IField[]>(_ => TheFieldsMatch(_, fields) ), Arg.Is<string[]>(_ => _.EqualTo(relationships)))
                .Returns(entities);
        }

        private bool TheFieldsMatch(IField[] actual,
            (string fieldName, string fieldValue)[] fields)
        {
            actual
                .Should()
                .BeEquivalentTo<IField>(fields.Select(_ => new Field
                {
                    Name = _.fieldName,
                    Value = _.fieldValue
                }));

            return true;
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

        protected static TItem[] AsArray<TItem>(params TItem[] items) => items;
    }
}
