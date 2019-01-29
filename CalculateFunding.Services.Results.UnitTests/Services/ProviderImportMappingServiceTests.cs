using System;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public class ProviderImportMappingServiceTests
    {
        [TestMethod]
        public void Map_GivenMasterProviderModelWithNoUKPRNorURN_ReturnsNull()
        {
            //Arrange
            MasterProviderModel model = new MasterProviderModel();

            ProviderImportMappingService mappingService = new ProviderImportMappingService();

            //Act
            ProviderIndex providerIndex = mappingService.Map(model);

            //Assert
            providerIndex
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void Map_GivenMasterProviderModelWithUKPRN_SetsUKPrnAsIdentifier()
        {
            //Arrange
            MasterProviderModel model = new MasterProviderModel
            {
                MasterUKPRN = "1234"
            };

            ProviderImportMappingService mappingService = new ProviderImportMappingService();

            //Act
            ProviderIndex providerIndex = mappingService.Map(model);

            //Assert
            providerIndex
                .ProviderId
                .Should()
                .Be("1234");

            providerIndex
                .ProviderIdType
                .Should()
                .Be("UKPRN");
        }

        [TestMethod]
        public void Map_GivenMasterProviderModelWithNoUKPRNButURN_SetsURNAsIdentifier()
        {
            //Arrange
            MasterProviderModel model = new MasterProviderModel
            {
                MasterURN = "1234"
            };

            ProviderImportMappingService mappingService = new ProviderImportMappingService();

            //Act
            ProviderIndex providerIndex = mappingService.Map(model);

            //Assert
            providerIndex
                .ProviderId
                .Should()
                .Be("1234");

            providerIndex
                .ProviderIdType
                .Should()
                .Be("URN");
        }

        [TestMethod]
        public void Map_GivenMasterProviderModel_MapsAllProperties()
        {
            //Arrange
	        const EstablishmentClosedReason masterReasonEstablishmentClosed = EstablishmentClosedReason.ResultOfAmalgamation;
	        const EstablishmentOpenedReason masterReasonEstablishmentOpened = EstablishmentOpenedReason.ResultOfClosure;
			const string masterSuccessor = "12345678";

	        MasterProviderModel model = new MasterProviderModel
            {
                MasterCRMAccountId = "1",
                MasterDateClosed = DateTimeOffset.Now,
                MasterDateOpened = DateTimeOffset.Now,
                MasterDfEEstabNo = "111",
                MasterDfELAEstabNo = "222",
                MasterLocalAuthorityCode = "111111",
                MasterLocalAuthorityName = "Timbuktoo",
                MasterProviderLegalName = "legal name",
                MasterProviderName = "name",
                MasterProviderStatusName = "Active",
                MasterProviderTypeGroupName = "type",
                MasterProviderTypeName = "sub type",
                MasterUKPRN = "1234",
                MasterUPIN = "4321",
                MasterURN = "2413",
                MasterNavendorNo = "12345",
                MasterPhaseOfEducation = "0",
				MasterSuccessor = masterSuccessor,
				MasterReasonEstablishmentClosed = masterReasonEstablishmentClosed,
				MasterReasonEstablishmentOpened = masterReasonEstablishmentOpened
			};

            ProviderImportMappingService mappingService = new ProviderImportMappingService();

            //Act
            ProviderIndex providerIndex = mappingService.Map(model);

            //Assert
            providerIndex.CrmAccountId.Should().Be("1");
            providerIndex.CloseDate.Should().NotBeNull();
            providerIndex.OpenDate.Should().NotBeNull();
            providerIndex.DfeEstablishmentNumber.Should().Be("111");
            providerIndex.EstablishmentNumber.Should().Be("222");
            providerIndex.LACode.Should().Be("111111");
            providerIndex.Authority.Should().Be("Timbuktoo");
            providerIndex.LegalName.Should().Be("legal name");
            providerIndex.Name.Should().Be("name");
            providerIndex.Status.Should().Be("Active");
            providerIndex.ProviderType.Should().Be("type");
            providerIndex.ProviderSubType.Should().Be("sub type");
            providerIndex.UKPRN.Should().Be("1234");
            providerIndex.UPIN.Should().Be("4321");
            providerIndex.URN.Should().Be("2413");
            providerIndex.NavVendorNo.Should().Be("12345");
            providerIndex.PhaseOfEducation.Should().Be("0");
            providerIndex.ReasonEstablishmentClosed.Should().Be("Result of Amalgamation/Merger");
            providerIndex.ReasonEstablishmentOpened.Should().Be("Result of Closure");
            providerIndex.Successor.Should().Be(masterSuccessor);
        }
    }
}
