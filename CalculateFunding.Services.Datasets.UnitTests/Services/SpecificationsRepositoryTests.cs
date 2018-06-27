using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class SpecificationsRepositoryTests
    {
        [TestMethod]
        public void GetSpecificationById_GivenNullOrEmptyUrl_ThrowsArgumentException()
        {
            //Arrange
            SpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            //Act
            Func<Task> test = async () => await specificationsRepository.GetSpecificationSummaryById("");

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }


        [TestMethod]
        async public Task GetSpecificationById_GivenSpecificationId_CallsWithCorrectUrl()
        {
            //Arrange
            const string specificationId = "spec-id";

            ISpecificationsApiClientProxy clientProxy = CreateApiClientProxy();

            SpecificationsRepository specificationsRepository = CreateSpecificationsRepository(clientProxy);

            //Act
            await specificationsRepository.GetSpecificationSummaryById(specificationId);

            //Assert
            await
                clientProxy
                    .Received(1)
                    .GetAsync<SpecificationSummary>(Arg.Is($"specs/specification-summary-by-id?specificationId={specificationId}"));
        }

        static SpecificationsRepository CreateSpecificationsRepository(ISpecificationsApiClientProxy apiClientProxy = null)
        {
            return new SpecificationsRepository(apiClientProxy ?? CreateApiClientProxy());
        }

        static ISpecificationsApiClientProxy CreateApiClientProxy()
        {
            return Substitute.For<ISpecificationsApiClientProxy>();
        }
    }
}
