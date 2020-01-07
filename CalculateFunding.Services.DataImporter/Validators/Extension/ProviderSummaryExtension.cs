using System;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;

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
            if (identifierType == IdentifierFieldType.LACode)
            {
                return providerSummary.LACode;
            }
            if (identifierType == IdentifierFieldType.EstablishmentNumber)
            {
                return providerSummary.EstablishmentNumber;
            }
            throw new ArgumentOutOfRangeException($"{nameof(identifierType)} was not one of the expected types");
        }
    }
}
