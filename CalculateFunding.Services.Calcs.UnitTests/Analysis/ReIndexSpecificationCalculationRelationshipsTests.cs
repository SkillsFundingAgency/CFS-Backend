using System;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Analysis;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    [TestClass]
    public class ReIndexSpecificationCalculationRelationshipsTests
    {
        private Mock<ISpecificationCalculationAnalysis> _analysis;
        private Mock<IReIndexGraphRepository> _relationships;
        private Mock<IJobManagement> _jobManagement;

        private Message _message;
        
        private ReIndexSpecificationCalculationRelationships _reIndexer;

        [TestInitialize]
        public void SetUp()
        {
            _analysis = new Mock<ISpecificationCalculationAnalysis>();
            _relationships = new Mock<IReIndexGraphRepository>();
            _jobManagement = new Mock<IJobManagement>();

            _message = new Message();
            
            _reIndexer = new ReIndexSpecificationCalculationRelationships(_analysis.Object,
                _relationships.Object,
                new Mock<ILogger>().Object,
                _jobManagement.Object);
        }

        [TestMethod]
        public void GuardsAgainstNoMessageBeingSupplied()
        {
            _message = null;

            Func<Task> invocation = WhenTheReIndexerIsRun;

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message");
        }
        
        [TestMethod]
        public void GuardsAgainstNoSpecificationIdPropertyInTheSuppliedMessage()
        {
            Func<Task> invocation = WhenTheReIndexerIsRun;

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task UpsertsRelationshipsFoundForSpecificationId()
        {
            string specificationId = new RandomString();
            string jobId = new RandomString();
            SpecificationCalculationRelationships specificationCalculationRelationships = new SpecificationCalculationRelationships();
            SpecificationCalculationRelationships specificationCalculationUnusedRelationships = new SpecificationCalculationRelationships();

            GivenTheMessageProperties(("specification-id", specificationId));
            GivenTheMessageProperties(("jobid", jobId));
            AndTheRelationshipsForSpecificationId(specificationId, specificationCalculationRelationships);
            AndTheUnusedRelationshipsForSpecificationId(specificationCalculationRelationships, specificationCalculationUnusedRelationships);

            await WhenTheReIndexerIsRun();
            
            _relationships.Verify(_ => _.RecreateGraph(specificationCalculationRelationships, specificationCalculationUnusedRelationships),
                Times.Once);
        }

        private void AndTheRelationshipsForSpecificationId(string specificationId,
            SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            _analysis.Setup(_ => _.GetSpecificationCalculationRelationships(specificationId))
                .ReturnsAsync(specificationCalculationRelationships);
        }

        private void AndTheUnusedRelationshipsForSpecificationId(SpecificationCalculationRelationships specificationCalculationRelationships,
            SpecificationCalculationRelationships specificationUnusedCalculationRelationships)
        {
            _relationships.Setup(_ => _.GetUnusedRelationships(specificationCalculationRelationships))
                .ReturnsAsync(specificationUnusedCalculationRelationships);
        }

        private async Task WhenTheReIndexerIsRun()
        {
            await _reIndexer.Run(_message);
        }

        private void GivenTheMessageProperties(params (string, string)[] properties)
        {
            foreach ((string, string) property in properties)
            {
                _message.UserProperties.Add(property.Item1, property.Item2);
            }
        }
    }
}