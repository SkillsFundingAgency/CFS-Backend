using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class InstructAllocationJobCreationTests
    {
        private const string GenerateGraphAndInstructAllocationJob = JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob;
        private const string AssemblyEtag = "assembly-etag";
        
        private Mock<IJobManagement> _jobs;
        private Mock<ISourceFileRepository> _sourceCode;
        private Mock<ICalculationsFeatureFlag> _features;

        private InstructionAllocationJobCreation _instructionAllocationJobCreation;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _sourceCode = new Mock<ISourceFileRepository>();
            _features = new Mock<ICalculationsFeatureFlag>();

            _features.Setup(_ => _.IsGraphEnabled())
                .ReturnsAsync(true);

            _jobs.Setup(_ => _.QueueJob(It.IsAny<JobCreateModel>()))
                .ReturnsAsync(new Job());
            
            _instructionAllocationJobCreation = new InstructionAllocationJobCreation(new Mock<ICalculationsRepository>().Object,
                new ResiliencePolicies
                {
                    CalculationsRepository = Policy.NoOpAsync()
                },
                Logger.None,
                _features.Object,
                _jobs.Object,
                _sourceCode.Object);
        }

        [TestMethod]
        public async Task AddAssemblyETagToInstructAllocationJobsWhenLocatedForSpecification()
        {
            string expectedETag = NewRandomString();
            string specificationId = NewRandomString();

            GivenTheEtag(specificationId, expectedETag);

            await WhenTheInstructAllocationMessageIsCreated(specificationId);
            
            ThenTheInstructionAllocationJobHadTheETagProperty(expectedETag);
        }

        [TestMethod]
        public async Task OmitsAssemblyETagFromInstructAllocationJobsWhenNotLocatedForSpecification()
        {
            string specificationId = NewRandomString();

            await WhenTheInstructAllocationMessageIsCreated(specificationId);
            
            ThenTheInstructionAllocationJobHadNoETagProperty();
        }

        private void ThenTheInstructionAllocationJobHadTheETagProperty(string etag)
        {
            _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job =>
                job.JobDefinitionId == GenerateGraphAndInstructAllocationJob &&
                job.Properties.ContainsKey(AssemblyEtag) &&
                job.Properties[AssemblyEtag] == etag)),
                Times.Once);
        }

        private void ThenTheInstructionAllocationJobHadNoETagProperty()
        {
            _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job =>
                    job.JobDefinitionId == GenerateGraphAndInstructAllocationJob &&
                    !job.Properties.ContainsKey(AssemblyEtag))),
                Times.Once);
        }

        private async Task WhenTheInstructAllocationMessageIsCreated(string specificationId)
        {
            await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specificationId,
                NewRandomString(),
                NewRandomString(),
                new Trigger(),
                NewRandomString());
        }

        private void GivenTheEtag(string specificationId,
            string etag)
            => _sourceCode.Setup(_ => _.GetAssemblyETag(specificationId))
                .ReturnsAsync(etag);

        private static string NewRandomString() => new RandomString();
    }
}