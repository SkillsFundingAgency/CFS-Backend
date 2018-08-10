using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.DataImporter.Validators.Extension
{
    public static class ProviderSummaryExtension
    {
	    public static string GetIdentifierBasedOnIdentifierType(this ProviderSummary providerSummary, IdentifierFieldType? identifierType)
	    {
			if (identifierType == IdentifierFieldType.UKPRN)
			{
				return providerSummary.UKPRN;
			}
			if (identifierType == IdentifierFieldType.UPIN)
			{
				return providerSummary.UPIN;
			}
			if (identifierType == IdentifierFieldType.URN)
			{
				return providerSummary.URN;
			}

		    throw new ArgumentOutOfRangeException($"{nameof(identifierType)} was not one of the expected types");
	    }
    }
}
