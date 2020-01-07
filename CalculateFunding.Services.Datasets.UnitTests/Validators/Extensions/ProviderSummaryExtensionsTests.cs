using System;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.Extensions
{
	[TestClass]
    public class ProviderSummaryExtensionsTests
    {
	    [TestMethod]
	    public void GetIdentifierBasedOnIdentifierType_GivenIdentifierTypeOfUkprn_ShouldReturnUkprn()
	    {
			// Arrange
		    var providerSummaryUnderTest = new ProviderSummary
		    {
			    Authority = "Barnsley",
			    CrmAccountId = null,
			    DateOpened = null,
			    EstablishmentNumber = "8001",
			    Id = null,
			    LACode = null,
			    LegalName = null,
			    Name = "Barnsley College",
			    NavVendorNo = null,
			    ProviderProfileIdType = null,
			    ProviderSubType = "General FE and Tertiary",
			    ProviderType = "16-18 Provider",
			    UKPRN = "12345678",
			    UPIN = "107013",
			    URN = "130524"
		    };
			
			// Act
		    string result = providerSummaryUnderTest.GetIdentifierBasedOnIdentifierType(IdentifierFieldType.UKPRN);

		    // Assert
			result.Should().BeEquivalentTo(providerSummaryUnderTest.UKPRN);
	    }

	    [TestMethod]
	    public void GetIdentifierBasedOnIdentifierType_GivenIdentifierTypeOfUpin_ShouldReturnUpin()
	    {
		    // Arrange
		    var providerSummaryUnderTest = new ProviderSummary
		    {
			    Authority = "Barnsley",
			    CrmAccountId = null,
			    DateOpened = null,
			    EstablishmentNumber = "8001",
			    Id = null,
			    LACode = null,
			    LegalName = null,
			    Name = "Barnsley College",
			    NavVendorNo = null,
			    ProviderProfileIdType = null,
			    ProviderSubType = "General FE and Tertiary",
			    ProviderType = "16-18 Provider",
			    UKPRN = "12345678",
			    UPIN = "107013",
			    URN = "130524"
		    };

		    // Act
		    string result = providerSummaryUnderTest.GetIdentifierBasedOnIdentifierType(IdentifierFieldType.UPIN);

		    // Assert
		    result.Should().BeEquivalentTo(providerSummaryUnderTest.UPIN);
	    }

	    [TestMethod]
	    public void GetIdentifierBasedOnIdentifierType_GivenIdentifierTypeOfUrn_ShouldReturnUrn()
	    {
		    // Arrange
		    var providerSummaryUnderTest = new ProviderSummary
		    {
			    Authority = "Barnsley",
			    CrmAccountId = null,
			    DateOpened = null,
			    EstablishmentNumber = "8001",
			    Id = null,
			    LACode = null,
			    LegalName = null,
			    Name = "Barnsley College",
			    NavVendorNo = null,
			    ProviderProfileIdType = null,
			    ProviderSubType = "General FE and Tertiary",
			    ProviderType = "16-18 Provider",
			    UKPRN = "12345678",
			    UPIN = "107013",
			    URN = "130524"
		    };

		    // Act
		    string result = providerSummaryUnderTest.GetIdentifierBasedOnIdentifierType(IdentifierFieldType.URN);

		    // Assert
		    result.Should().BeEquivalentTo(providerSummaryUnderTest.URN);
	    }

	    [TestMethod]
	    public void GetIdentifierBasedOnIdentifierType_GivenAnUnexpectedIdentifier_ShouldThrowException()
	    {
		    // Arrange
		    var providerSummaryUnderTest = new ProviderSummary
		    {
			    Authority = "Barnsley",
			    CrmAccountId = null,
			    DateOpened = null,
			    EstablishmentNumber = "8001",
			    Id = null,
			    LACode = null,
			    LegalName = null,
			    Name = "Barnsley College",
			    NavVendorNo = null,
			    ProviderProfileIdType = null,
			    ProviderSubType = "General FE and Tertiary",
			    ProviderType = "16-18 Provider",
			    UKPRN = "12345678",
			    UPIN = "107013",
			    URN = "130524"
		    };

		    // Act
		    Action getIdentifier = () =>
		    {
			    providerSummaryUnderTest.GetIdentifierBasedOnIdentifierType(IdentifierFieldType.None);
		    };

		    // Assert
		    getIdentifier.Should().Throw<ArgumentOutOfRangeException>();
	    }

	    
	}
}
