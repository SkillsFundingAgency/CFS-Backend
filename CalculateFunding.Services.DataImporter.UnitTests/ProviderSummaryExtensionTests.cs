using CalculateFunding.Models.Datasets.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Services.Scenarios
{
    [TestClass]
    public class ProviderSummaryExtensionTests
    {
        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndUkprnIdentifier_ReturnsUkPrnValue()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.UKPRN;

            //Act
            string identifierValue = providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            identifierValue
                .Should()
                .Be("12345");
        }

        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndUpinIdentifier_ReturnsUpinValue()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.UPIN;

            //Act
            string identifierValue = providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            identifierValue
                .Should()
                .Be("1234");
        }

        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndUrnIdentifier_ReturnsUrnValue()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.URN;

            //Act
            string identifierValue = providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            identifierValue
                .Should()
                .Be("4321");
        }

        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndLaCodeIdentifier_ReturnsLaCodeValue()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.LACode;

            //Act
            string identifierValue = providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            identifierValue
                .Should()
                .Be("111");
        }

        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndEstablishmentNumberIdentifier_ReturnsEstablishmentNumberValue()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.EstablishmentNumber;

            //Act
            string identifierValue = providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            identifierValue
                .Should()
                .Be("444444");
        }

        [TestMethod]
        public void GetIdentifierBasedOnIdentifierType_GivenSummaryAndNoneIdentifier_ThrowsArgumentOutOfRangeException()
        {
            //Arrange
            ProviderSummary providerSummary = CreateProviderSummary();

            IdentifierFieldType identifierFieldType = IdentifierFieldType.None;

            //Act
            Action test = () => providerSummary.GetIdentifierBasedOnIdentifierType(identifierFieldType);

            //Assert
            test
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .Message
                .Should()
                .Be("Specified argument was out of the range of valid values.\r\nParameter name: identifierType was not one of the expected types");
        }

        public static ProviderSummary CreateProviderSummary()
        {
            return new ProviderSummary
            {
                UKPRN = "12345",
                UPIN = "1234",
                URN = "4321",
                LACode = "111",
                EstablishmentNumber = "444444"
            };
        }
    }
}
