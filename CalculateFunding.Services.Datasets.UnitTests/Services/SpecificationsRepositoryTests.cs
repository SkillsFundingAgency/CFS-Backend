using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
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
            Func<Task> test = async () => await specificationsRepository.GetSpecificationById("");

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }


        [TestMethod]
        async public Task GetSpecificationById_GivenSpecificationId_CallsWithCorrectUrl()
        {
            //Arrange
            const string specificationId = "spec-id";

            IApiClientProxy clientProxy = CreateApiClientProxy();

            SpecificationsRepository specificationsRepository = CreateSpecificationsRepository(clientProxy);

            //Act
            await specificationsRepository.GetSpecificationById(specificationId);

            //Assert
            await
                clientProxy
                    .Received(1)
                    .GetAsync<Specification>(Arg.Is($"specs/specifications?specificationId={specificationId}"));
        }

        static SpecificationsRepository CreateSpecificationsRepository(IApiClientProxy apiClientProxy = null)
        {
            return new SpecificationsRepository(apiClientProxy ?? CreateApiClientProxy());
        }

        static IApiClientProxy CreateApiClientProxy()
        {
            return Substitute.For<IApiClientProxy>();
        }
    }
}
