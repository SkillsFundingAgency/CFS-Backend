﻿using System;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    [TestClass]
    public class SpecificationRepositoryTests
    {
        [TestMethod]
        public void SpecificationsRepositoryCtor_WhenApiClientIsNull_ShouldThrowException()
        {
            // Arrange
            ISpecificationsApiClientProxy nullApiClient = null;

            // Act
            Action specificationRepositoryCtor = () => { new SpecificationsRepository(nullApiClient); };

            // Assert
            specificationRepositoryCtor.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void GetSpecificationSummaryById_WhenGivenAnEmptySpecificationId_ShouldThrowAnException()
        {
            // Arrange
            ISpecificationsApiClientProxy mockApiClient = Substitute.For<ISpecificationsApiClientProxy>();
            
            // Act
            SpecificationsRepository specificationRepo = new SpecificationsRepository(mockApiClient);
            Action getSpecificationBySummaryIdCall = () => { specificationRepo.GetSpecificationSummaryById(string.Empty);};

            // Assert
            getSpecificationBySummaryIdCall.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void GetSpecificationSummaryById_WhenGivenANullSpecificationId_ShouldThrowAnException()
        {
            // Arrange
            ISpecificationsApiClientProxy mockApiClient = Substitute.For<ISpecificationsApiClientProxy>();

            // Act
            SpecificationsRepository specificationRepo = new SpecificationsRepository(mockApiClient);
            Action getSpecificationBySummaryIdCall = () => { specificationRepo.GetSpecificationSummaryById(null);};

            // Assert
            getSpecificationBySummaryIdCall.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void GetSpecificationSummaryById_WhenGivenAValidSpecificationId_ShouldCallApiClient()
        {
            // Arrange
            ISpecificationsApiClientProxy mockApiClient = Substitute.For<ISpecificationsApiClientProxy>();
            const string specificationId = "validSpecId";

            // Act
            SpecificationsRepository specificationRepo = new SpecificationsRepository(mockApiClient);
            specificationRepo.GetSpecificationSummaryById(specificationId).Wait();

            // Assert
            mockApiClient.Received(1).GetAsync<SpecificationSummary>(Arg.Any<string>());
        }
    }
}
