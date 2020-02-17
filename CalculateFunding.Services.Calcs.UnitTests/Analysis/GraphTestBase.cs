using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphTestBase
    {
        protected Mock<IMapper> Mapper;

        [TestInitialize]
        public void GraphTestBaseSetUp()
        {
            Mapper = new Mock<IMapper>();
        }

        protected SpecificationCalculationRelationships NewSpecificationCalculationRelationships(
            Action<SpecificationCalculationRelationshipBuilder> setUp = null)
        {
            SpecificationCalculationRelationshipBuilder builder = new SpecificationCalculationRelationshipBuilder();

            setUp?.Invoke(builder);
            
            return builder.Build();
        }

        protected CalculationRelationship NewCalculationRelationship(Action<CalculationRelationshipsBuilder> setUp = null)
        {
            CalculationRelationshipsBuilder builder = new CalculationRelationshipsBuilder();
           
            setUp?.Invoke(builder);

            return builder.Build();
        }

        protected Specification NewGraphSpecification(Action<GraphSpecificationBuilder> setUp = null)
        {
            GraphSpecificationBuilder builder = new GraphSpecificationBuilder();
            
            setUp?.Invoke(builder);

            return builder.Build();
        }

        protected Calculation NewGraphCalculation(Action<GraphCalculationBuilder> setUp = null)
        {
            GraphCalculationBuilder builder = new GraphCalculationBuilder();

            setUp?.Invoke(builder);
            
            return builder.Build();
        }
        
        protected void GivenTheMapping<TSource, TDestination>(TSource source, TDestination destination)
        {
            Mapper.Setup(_ => _.Map<TDestination>(source))
                .Returns(destination);
        }

        protected void AndTheMapping<TSource, TDestination>(TSource source, TDestination destination)
        {
            GivenTheMapping(source, destination);
        }
        
        protected void AndTheCollectionMapping<TSource, TDestination>(IEnumerable<TSource> source, IEnumerable<TDestination> destination)
        {
            Mapper.Setup(_ => _.Map<IEnumerable<TDestination>>(It.Is<IEnumerable<TSource>>(src => src.SequenceEqual(source))))
                .Returns(destination);
        }

        protected static string NewRandomString() => new RandomString();
    }
}