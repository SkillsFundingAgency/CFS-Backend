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
        
        private Mock<IJobManagement> _jobs;
        private Mock<ICalculationsFeatureFlag> _features;

        private InstructionAllocationJobCreation _instructionAllocationJobCreation;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
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
                _jobs.Object);
        }

    }
}