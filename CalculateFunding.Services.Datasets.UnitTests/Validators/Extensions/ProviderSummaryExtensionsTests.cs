using System;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
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
			result.ShouldBeEquivalentTo(providerSummaryUnderTest.UKPRN);
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
		    result.ShouldBeEquivalentTo(providerSummaryUnderTest.UPIN);
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
		    result.ShouldBeEquivalentTo(providerSummaryUnderTest.URN);
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
			    providerSummaryUnderTest.GetIdentifierBasedOnIdentifierType(IdentifierFieldType.Authority);
		    };

		    // Assert
		    getIdentifier.ShouldThrow<ArgumentOutOfRangeException>();
	    }

	    
	}
}
