using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class DeleteSpecificationServiceTests
    {
        private ICreateDeleteSpecificationJobs _deleteSpecifications;
        private DeleteSpecificationService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _deleteSpecifications = Substitute.For<ICreateDeleteSpecificationJobs>();
            
            _service = new DeleteSpecificationService(_deleteSpecifications);
        }

        [TestMethod]
        public async Task QueueDeleteJobDelegatesToDeleteJobCreationService()
        {
            string correlationId = NewRandomString();
            string specificationId = NewRandomString();
            Reference user = NewUser();

            await _service.QueueDeleteSpecificationJob(specificationId, user, correlationId);

            await _deleteSpecifications
                .Received(1)
                .CreateJob(Arg.Is(specificationId),
                    Arg.Is(user),
                    Arg.Is(correlationId),
                    null,
                    null);
        }

        private Reference NewUser(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}