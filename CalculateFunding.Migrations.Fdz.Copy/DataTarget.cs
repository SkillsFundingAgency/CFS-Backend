using CalculateFunding.Migrations.Fdz.Copy.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CalculateFunding.Migrations.Fdz.Copy
{
    internal class DataTarget
    {
        private CopyOptions _options;

        public DataTarget(CopyOptions options)
        {
            _options = options;
        }

        internal int AddSnapshot(ProviderSnapshot providerSnapshot)
        {
            providerSnapshot.Name = _options.TargetSnapshotName ?? providerSnapshot.Name;
            providerSnapshot.TargetDate = _options.TargetSnapshotDate ?? providerSnapshot.TargetDate;
            providerSnapshot.Description = _options.TargetSnapshotDescription ?? providerSnapshot.Description;
            providerSnapshot.Version = _options.TargetSnapshotVersion > 0 ? _options.TargetSnapshotVersion : providerSnapshot.Version;
            providerSnapshot.FundingStreamId = _options.TargetSnapshotFundingStreamId ?? providerSnapshot.FundingStreamId;

            using (SqlConnection connection = new SqlConnection(_options.TargetConnectionString))
            {
                string query = @"insert into ProviderSnapshot
                                        (
                                            [Name],
                                            [Description],
                                            [Version],
                                            [TargetDate],
                                            [FundingStreamId],
                                            [Created]
                                        )
                                    values
                                        (
                                            @Name,
                                            @Description,
                                            @Version,
                                            @TargetDate,
                                            @FundingStreamId,
                                            @Created
                                        );
                                    SELECT CAST(SCOPE_IDENTITY() AS INT)";

                return connection.QuerySingle<int>(query, providerSnapshot);
            }
        }

        internal int AddSnapshotPeriod(int providerSnapshotId, ProviderSnapshotPeriod providerSnapshotPeriod)
        {
            var providerSnapshotPeriodAddition = new ProviderSnapshotPeriod
            {
                ProviderSnapshotId = providerSnapshotId,
                FundingPeriodName = providerSnapshotPeriod.FundingPeriodName
            };


            using (SqlConnection connection = new SqlConnection(_options.TargetConnectionString))
            {
                string query = @"insert into ProviderSnapshotPeriod
                                        (
                                            [ProviderSnapshotId],
                                            [FundingPeriodName]
                                        )
                                    values
                                        (
                                            @ProviderSnapshotId,
                                            @FundingPeriodName
                                        );";

                return connection.Execute(query, providerSnapshotPeriodAddition);
            }
        }

        internal Dictionary<int, int> AddPaymentOrganisations(int snapshotId, List<PaymentOrganisation> paymentOrganisations)
        {
            Dictionary<int, int> lookup = new Dictionary<int, int>();

            using (SqlConnection connection = new SqlConnection(_options.TargetConnectionString))
            {
                string query = @"insert into PaymentOrganisation
                                        (
                                            [ProviderSnapshotId],
                                            [Name],
                                            [Ukprn],
                                            [Upin],
                                            [TrustCode],
                                            [LaCode],
                                            [LaOrg],
                                            [Urn],
                                            [PaymentOrganisationType],
                                            [CompanyHouseNumber]
                                        )
                                    values
                                        (
                                            @ProviderSnapshotId,
                                            @Name,
                                            @Ukprn,
                                            @Upin,
                                            @TrustCode,
                                            @LaCode,
                                            @LaOrg,
                                            @Urn,
                                            @PaymentOrganisationType,
                                            @CompanyHouseNumber
                                        );
                                    SELECT CAST(SCOPE_IDENTITY() AS INT)";

                foreach (PaymentOrganisation paymentOrganisation in paymentOrganisations)
                {
                    paymentOrganisation.ProviderSnapshotId = snapshotId;
                    int id = connection.QuerySingle<int>(query, paymentOrganisation);

                    lookup.Add(paymentOrganisation.PaymentOrganisationId, id);
                }

                return lookup;
            }
        }

        internal Dictionary<int, int> AddProviders(int snapshotId, Dictionary<int, int> paymentOrganisationLookup, List<Provider> providers)
        {
            Dictionary<int, int> lookup = new Dictionary<int, int>();

            using (SqlConnection connection = new SqlConnection(_options.TargetConnectionString))
            {
                string query = @"insert into Provider
                                        (
                                            [ProviderSnapshotId],
                                            [ProviderId],
                                            [Name],
                                            [Urn],
                                            [Ukprn],
                                            [Upin],
                                            [EstablishmentNumber],
                                            [DfeEstablishmentNumber],
                                            [Authority],
                                            [ProviderType],
                                            [ProviderSubType],
                                            [DateOpened],
                                            [DateClosed],
                                            [ProviderProfileIdType],
                                            [LaCode],
                                            [LaOrg],
                                            [NavVendorNo],
                                            [CrmAccountId],
                                            [LegalName],
                                            [PhaseOfEducation],
                                            [ReasonEstablishmentOpened],
                                            [ReasonEstablishmentClosed],
                                            [Town],
                                            [Postcode],
                                            [TrustName],
                                            [TrustCode],
                                            [CompaniesHouseNumber],
                                            [GroupIdNumber],
                                            [RscRegionName],
                                            [RscRegionCode],
                                            [GovernmentOfficeRegionName],
                                            [GovernmentOfficeRegionCode],
                                            [DistrictName],
                                            [DistrictCode],
                                            [WardName],
                                            [WardCode],
                                            [CensusWardName],
                                            [CensusWardCode],
                                            [MiddleSuperOutputAreaName],
                                            [MiddleSuperOutputAreaCode],
                                            [LowerSuperOutputAreaName],
                                            [LowerSuperOutputAreaCode],
                                            [ParliamentaryConstituencyName],
                                            [ParliamentaryConstituencyCode],
                                            [CountryCode],
                                            [CountryName],
                                            [LocalGovernmentGroupTypeCode]
                                            ,[LocalGovernmentGroupTypeName]
                                            ,[TrustStatus]
                                            ,[ProviderStatusId]
                                            ,[PaymentOrganisationId]
                                            ,[Street]
                                            ,[Locality]
                                            ,[Address3]
                                            ,[ProviderTypeCode]
                                            ,[ProviderSubTypeCode]
                                            ,[StatusCode]
                                            ,[ReasonEstablishmentOpenedCode]
                                            ,[ReasonEstablishmentClosedCode]
                                            ,[PhaseOfEducationCode]
                                            ,[StatutoryLowAge]
                                            ,[StatutoryHighAge]
                                            ,[OfficialSixthFormCode]
                                            ,[OfficialSixthFormName]
                                            ,[PreviousLaCode]
                                            ,[PreviousLaName]
                                            ,[PreviousEstablishmentNumber]
                                            ,[FurtherEducationTypeCode]
                                            ,[FurtherEducationTypeName]
                                            ,[LondonRegionName]
                                            ,[LondonRegionCode]
                                        )
                                    values
                                        (
                                            @ProviderSnapshotId,
                                            @ProviderId,
                                            @Name,
                                            @Urn,
                                            @Ukprn,
                                            @Upin,
                                            @EstablishmentNumber,
                                            @DfeEstablishmentNumber,
                                            @Authority,
                                            @ProviderType,
                                            @ProviderSubType,
                                            @DateOpened,
                                            @DateClosed,
                                            @ProviderProfileIdType,
                                            @LaCode,
                                            @LaOrg,
                                            @NavVendorNo,
                                            @CrmAccountId,
                                            @LegalName,
                                            @PhaseOfEducation,
                                            @ReasonEstablishmentOpened,
                                            @ReasonEstablishmentClosed,
                                            @Town,
                                            @Postcode,
                                            @TrustName,
                                            @TrustCode,
                                            @CompaniesHouseNumber,
                                            @GroupIdNumber,
                                            @RscRegionName,
                                            @RscRegionCode,
                                            @GovernmentOfficeRegionName,
                                            @GovernmentOfficeRegionCode,
                                            @DistrictName,
                                            @DistrictCode,
                                            @WardName,
                                            @WardCode,
                                            @CensusWardName,
                                            @CensusWardCode,
                                            @MiddleSuperOutputAreaName,
                                            @MiddleSuperOutputAreaCode,
                                            @LowerSuperOutputAreaName,
                                            @LowerSuperOutputAreaCode,
                                            @ParliamentaryConstituencyName,
                                            @ParliamentaryConstituencyCode,
                                            @CountryCode,
                                            @CountryName,
                                            @LocalGovernmentGroupTypeCode
                                            ,@LocalGovernmentGroupTypeName
                                            ,@TrustStatus
                                            ,@ProviderStatusId
                                            ,@PaymentOrganisationId
                                            ,@Street
                                            ,@Locality
                                            ,@Address3
                                            ,@ProviderTypeCode
                                            ,@ProviderSubTypeCode
                                            ,@StatusCode
                                            ,@ReasonEstablishmentOpenedCode
                                            ,@ReasonEstablishmentClosedCode
                                            ,@PhaseOfEducationCode
                                            ,@StatutoryLowAge
                                            ,@StatutoryHighAge
                                            ,@OfficialSixthFormCode
                                            ,@OfficialSixthFormName
                                            ,@PreviousLaCode
                                            ,@PreviousLaName
                                            ,@PreviousEstablishmentNumber
                                            ,@FurtherEducationTypeCode
                                            ,@FurtherEducationTypeName
                                            ,@LondonRegionName
                                            ,@LondonRegionCode
                                        );
                                    SELECT CAST(SCOPE_IDENTITY() AS INT)";

                foreach (Provider provider in providers)
                {
                    provider.ProviderSnapshotId = snapshotId;
                    provider.PaymentOrganisationId = provider.PaymentOrganisationId == null ? null : paymentOrganisationLookup[(int)provider.PaymentOrganisationId];
                    int id = connection.QuerySingle<int>(query, provider);

                    lookup.Add(provider.Id, id);
                }

                return lookup;
            }
        }

        internal Dictionary<int, int> AddProviderRelationships<T>(Dictionary<int, int> providerLookup, List<T> relationships)
             where T : ProviderRelationship
        {
            Dictionary<int, int> lookup = new Dictionary<int, int>();

            using (SqlConnection connection = new SqlConnection(_options.TargetConnectionString))
            {
                string query = $"insert into {relationships.First().TableName} (ProviderId, UKPRN) values (@ProviderId, @Ukprn);SELECT CAST(SCOPE_IDENTITY() AS INT)";

                foreach (ProviderRelationship relationship in relationships)
                {
                    relationship.ProviderId = providerLookup[relationship.ProviderId];
                    int id = connection.QuerySingle<int>(query, relationship);

                    lookup.Add(relationship.Id, id);
                }

                return lookup;
            }
        }
    }
}
