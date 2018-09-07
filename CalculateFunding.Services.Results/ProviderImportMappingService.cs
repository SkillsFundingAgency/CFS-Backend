using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ProviderImportMappingService : IProviderImportMappingService
    {
        public ProviderIndex Map(MasterProviderModel masterProviderModel)
        {
            ProviderIndex providerIndex = new ProviderIndex();

            if (string.IsNullOrWhiteSpace(masterProviderModel.MasterUKPRN) && string.IsNullOrWhiteSpace(masterProviderModel.MasterURN))
            {
                return null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(masterProviderModel.MasterUKPRN))
                {
                    providerIndex.ProviderId = masterProviderModel.MasterUKPRN;
                    providerIndex.ProviderIdType = IdentifierFieldType.UKPRN.ToString();
                }
                else
                {
                    providerIndex.ProviderId = masterProviderModel.MasterURN;
                    providerIndex.ProviderIdType = IdentifierFieldType.URN.ToString();
                }

                providerIndex.Authority = masterProviderModel.MasterLocalAuthorityName;
                providerIndex.CloseDate = masterProviderModel.MasterDateClosed;
                providerIndex.DfeEstablishmentNumber = masterProviderModel.MasterDfEEstabNo;
                providerIndex.EstablishmentNumber = masterProviderModel.MasterDfELAEstabNo;
                providerIndex.LACode = masterProviderModel.MasterLocalAuthorityCode;
                providerIndex.LegalName = masterProviderModel.MasterProviderLegalName;
                providerIndex.Name = masterProviderModel.MasterProviderName;
                providerIndex.OpenDate = masterProviderModel.MasterDateOpened;
                providerIndex.ProviderSubType = masterProviderModel.MasterProviderTypeName;
                providerIndex.ProviderType = masterProviderModel.MasterProviderTypeGroupName;
                providerIndex.UKPRN = masterProviderModel.MasterUKPRN;
                providerIndex.UPIN = masterProviderModel.MasterUPIN;
                providerIndex.URN = masterProviderModel.MasterURN;
                providerIndex.CrmAccountId = masterProviderModel.MasterCRMAccountId;
                providerIndex.Status = masterProviderModel.MasterProviderStatusName;
                providerIndex.NavVendorNo = masterProviderModel.MasterNavendorNo;
                providerIndex.PhaseOfEducation = masterProviderModel.MasterPhaseOfEducation;

                return providerIndex;
            }
        }
    }
}
