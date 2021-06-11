﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FundingDataZone
{
    public class PublishingAreaEditorRepository : PublishingAreaRepository, IPublishingAreaEditorRepository
    {
        public PublishingAreaEditorRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory) : base(connectionFactory, sqlPolicyFactory)
        {
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return QuerySql<FundingStream>("SELECT * FROM FundingStream ORDER BY FundingStreamName ASC");
        }

        public async Task<FundingStream> CreateFundingStream(FundingStream fundingStream)
        {
            fundingStream.FundingStreamId = await Insert(fundingStream);
            return fundingStream;
        }

        public async Task<bool> UpdateProvider(PublishingAreaProvider publishingAreaProvider)
        {
            return await Update(publishingAreaProvider);
        }

		public async Task<PublishingAreaProvider> InsertProvider(PublishingAreaProvider publishingAreaProvider)
		{
			publishingAreaProvider.Id = await Insert(publishingAreaProvider);
			return publishingAreaProvider;
		}

		public async Task<bool> UpdateOrganisation(PublishingAreaOrganisation publishingAreaOrganisation)
        {
            return await Update(publishingAreaOrganisation);
        }

		public async Task<PublishingAreaOrganisation> InsertOrganisation(PublishingAreaOrganisation publishingAreaOrganisation)
		{
			publishingAreaOrganisation.PaymentOrganisationId = await Insert(publishingAreaOrganisation);
			return publishingAreaOrganisation;
		}

		public async Task<bool> UpdateProviderSnapshot(ProviderSnapshotTableModel providerSnapshotTableModel)
        {
            return await Update(providerSnapshotTableModel);
        }

        public Task<IEnumerable<ProviderStatus>> GetProviderStatuses()
        {
            return QuerySql<ProviderStatus>("SELECT * FROM ProviderStatus ORDER BY ProviderStatusName ASC");
        }

        public async Task<ProviderStatus> CreateProviderStatus(ProviderStatus providerStatus)
        {
            providerStatus.ProviderStatusId = await Insert(providerStatus);
            return providerStatus;
        }

        public async Task<int> CloneProviderSnapshot(int providerSnapshotId, string cloneName)
        {
			int cloneId = await QuerySingleSql<int>(@"BEGIN
    BEGIN TRANSACTION

	INSERT INTO ProviderSnapshot(
		[Name], 
		[Description], 
		[Version], 
		[TargetDate], 
		[FundingStreamId], 
		[Created]
	) 
	SELECT 
		@cloneName, 
		ps.[Description], 
		1, 
		GETDATE(), 
		ps.[FundingStreamId], 
		GETDATE() 
	FROM ProviderSnapshot ps 
	WHERE ps.[ProviderSnapshotId] = @providerSnapShot

	IF @@ERROR <> 0
	BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		RAISERROR ('Error inserting provider snapshot.', 16, 1)
		SELECT 0
		RETURN
	END

	declare @cloneProviderSnapshot int
	SET @cloneProviderSnapshot = SCOPE_IDENTITY()

	INSERT INTO PaymentOrganisation
		(
			[ProviderSnapshotId], 
			[Name], 
			[Ukprn], 
			[Upin], 
			[TrustCode], 
			[LaCode], 
			[Urn], 
			[PaymentOrganisationType], 
			[CompanyHouseNumber]
		) 
	SELECT 
		@cloneProviderSnapshot, 
		po.[Name], 
		po.[Ukprn], 
		po.[Upin], 
		po.[TrustCode], 
		po.[LaCode], 
		po.[Urn], 
		po.[PaymentOrganisationType], 
		po.[CompanyHouseNumber]
	FROM 
		PaymentOrganisation po 
	WHERE po.[ProviderSnapshotId] = @providerSnapShot

	IF @@ERROR <> 0
	BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		RAISERROR ('Error inserting payment organisations.', 16, 1)
		SELECT 0
		RETURN
	END

	INSERT INTO Provider
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
		[NavVendorNo],
		[CrmAccountId],
		[LegalName],
		[Status],
		[PhaseOfEducation],
		[ReasonEstablishmentOpened],
		[ReasonEstablishmentClosed],
		[Successor],
		[Town],
		[Postcode],
		[TrustName],
		[TrustCode],
		[LocalAuthorityName],
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
		[TrustStatus],
		[PaymentOrganisationId],
		[Street],
		[Locality],
		[Address3],
		[ProviderTypeCode],
		[ProviderSubTypeCode],
		[StatusCode],
		[ReasonEstablishmentOpenedCode],
		[ReasonEstablishmentClosedCode],
		[PhaseOfEducationCode],
		[StatutoryLowAge],
		[StatutoryHighAge],
		[OfficialSixthFormCode],
		[OfficialSixthFormName],
		[PreviousLaCode],
		[PreviousLaName],
		[PreviousEstablishmentNumber],
		[ProviderStatusId]
	)
	SELECT
		@cloneProviderSnapshot,
		p.[ProviderId], 
		p.[Name], 
		p.[Urn], 
		p.[Ukprn], 
		p.[Upin], 
		p.[EstablishmentNumber], 
		p.[DfeEstablishmentNumber],
		p.[Authority],
		p.[ProviderSubType],
		p.[ProviderSubType],
		p.[DateOpened],
		p.[DateClosed],
		p.[ProviderProfileIdType],
		p.[LaCode],
		p.[NavVendorNo],
		p.[CrmAccountId],
		p.[LegalName],
		p.[Status],
		p.[PhaseOfEducation],
		p.[ReasonEstablishmentOpened],
		p.[ReasonEstablishmentClosed],
		p.[Successor],
		p.[Town],
		p.[Postcode],
		p.[TrustName],
		p.[TrustCode],
		p.[LocalAuthorityName],
		p.[CompaniesHouseNumber],
		p.[GroupIdNumber],
		p.[RscRegionName],
		p.[RscRegionCode],
		p.[GovernmentOfficeRegionName],
		p.[GovernmentOfficeRegionCode],
		p.[DistrictName],
		p.[DistrictCode],
		p.[WardName],
		p.[WardCode],
		p.[CensusWardName],
		p.[CensusWardCode],
		p.[MiddleSuperOutputAreaName],
		p.[MiddleSuperOutputAreaCode],
		p.[LowerSuperOutputAreaName],
		p.[LowerSuperOutputAreaCode],
		p.[TrustStatus],
		clone.[PaymentOrganisationId],
		p.[Street],
		p.[Locality],
		p.[Address3],
		p.[ProviderTypeCode],
		p.[ProviderSubTypeCode],
		p.[StatusCode],
		p.[ReasonEstablishmentOpenedCode],
		p.[ReasonEstablishmentClosedCode],
		p.[PhaseOfEducationCode],
		p.[StatutoryLowAge],
		p.[StatutoryHighAge],
		p.[OfficialSixthFormCode],
		p.[OfficialSixthFormName],
		p.[PreviousLaCode],
		p.[PreviousLaName],
		p.[PreviousEstablishmentNumber],
		p.[ProviderStatusId]
	FROM
		Provider p
	INNER JOIN PaymentOrganisation po ON p.[PaymentOrganisationId] = po.[PaymentOrganisationId]
	INNER JOIN PaymentOrganisation clone ON clone.[Ukprn] = po.[Ukprn] AND clone.[ProviderSnapshotId] = @cloneProviderSnapshot
	WHERE
		p.[ProviderSnapshotId] = @providerSnapShot

	IF @@ERROR <> 0
	BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		RAISERROR ('Error inserting providers.', 16, 1)
		SELECT 0
		RETURN
	END

	COMMIT
	
	SELECT @cloneProviderSnapshot
END", new
			{
				cloneName = cloneName,
				providerSnapshot = providerSnapshotId
			});

			return cloneId;
		}

        public async Task DeletePredecessors(int providerId)
        {
            IEnumerable<Predecessor> existingPredecessors = await QuerySql<Predecessor>(@"SELECT Id FROM Predecessors
WHERE ProviderId = @providerId;
",
            new
            {
                providerId = providerId
            });

            foreach (Predecessor predecessor in existingPredecessors)
            {
                await Delete(predecessor);
            }
        }

        public async Task DeleteSuccessors(int providerId)
        {
            IEnumerable<Successor> existingSuccessors = await QuerySql<Successor>(@"SELECT Id FROM Successors
WHERE ProviderId = @providerId;
",
            new
            {
                providerId = providerId
            });

            foreach (Successor successor in existingSuccessors)
            {
                await Delete(successor);
            }
        }

        public async Task CreatePredecessors(IEnumerable<Predecessor> predecessors)
        {
            foreach (Predecessor predecessor in predecessors)
            {
                await Insert(predecessor);
            }
        }

        public async Task CreateSuccessors(IEnumerable<Successor> successors)
        {
            foreach (Successor successor in successors)
            {
                await Insert(successor);
            }
        }

        public Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshotsOrderedByTargetDate()
        {
            return QuerySql<PublishingAreaProviderSnapshot>(@"SELECT 
ps.[providerSnapshotId], 	
ps.[name], 	
ps.[description],
ps.[version],	
ps.[targetDate],	
ps.[created],	
fs.[fundingStreamName],	
fs.[fundingStreamCode]

FROM dbo.ProviderSnapshot ps
INNER JOIN dbo.FundingStream fs on fs.FundingStreamId = ps.FundingStreamId
ORDER BY ps.targetDate DESC");
        }

        public Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshotsOrderedByTargetDate(int fundingStreamId)
        {
            return QuerySql<PublishingAreaProviderSnapshot>(@"SELECT 
ps.[providerSnapshotId], 	
ps.[name], 	
ps.[description],
ps.[version],	
ps.[targetDate],	
ps.[created],	
fs.[fundingStreamName],	
fs.[fundingStreamCode]

FROM dbo.ProviderSnapshot ps
INNER JOIN dbo.FundingStream fs on fs.FundingStreamId = ps.FundingStreamId
WHERE ps.fundingStreamId = @fundingStreamId
ORDER BY ps.targetDate DESC", new { fundingStreamId = fundingStreamId });
        }

        public async Task<PublishingAreaOrganisation> GetOrganisationInSnapshot(int providerSnapshotId,
            string organisationId) =>
            await QuerySingleSql<PublishingAreaOrganisation>("Select * from PaymentOrganisation Where ProviderSnapshotId=@providerSnapshotId and PaymentOrganisationId=@paymentOrganisationId",
                new
                {
                    ProviderSnapshotId = providerSnapshotId,
                    paymentOrganisationId = organisationId
                });

        public async Task<ProviderSnapshotTableModel> CreateProviderSnapshot(ProviderSnapshotTableModel providerSnapshot)
        {
            providerSnapshot.ProviderSnapshotId = await Insert(providerSnapshot);

            return providerSnapshot;
        }

        public async Task<int> GetCountPaymentOrganisationsInSnapshot(int providerSnapshotId, string searchTerm = null)
        {
			searchTerm ??= "";
			searchTerm += "%";

			return await QuerySingleSql<int>(@"SELECT Count(*) FROM PaymentOrganisation
WHERE ProviderSnapshotId = @ProviderSnapshotId and UkPrn like @searchTerm",
            new
            {
                providerSnapshotId = providerSnapshotId,
				searchTerm = searchTerm
			});
        }

        public async Task<int> GetCountProvidersInSnapshot(int providerSnapshotId, string searchTerm = null)
        {
			searchTerm ??= "";
			searchTerm += "%";

			return await QuerySingleSql<int>(@"SELECT Count(*) FROM Provider
WHERE ProviderSnapshotId = @ProviderSnapshotId and UkPrn like @searchTerm",
            new
            {
                providerSnapshotId = providerSnapshotId,
				searchTerm = searchTerm
			});
        }

        public async Task<IEnumerable<PublishingAreaOrganisation>> GetPaymentOrganisationsInSnapshot(int providerSnapshotId, int pageNumber, int pageSize, string searchTerm)
		{
			searchTerm ??= "";
			searchTerm += "%";

			return await QuerySql<PublishingAreaOrganisation>(@"SELECT @PageSize ProviderId, Name, Ukprn, PaymentOrganisationId FROM PaymentOrganisation
WHERE ProviderSnapshotId = @ProviderSnapshotId and UkPrn like @searchTerm
ORDER BY Name
OFFSET ((@PageNumber - 1) * @RowspPage) ROWS
FETCH NEXT @RowspPage ROWS ONLY;
",
            new
            {
                providerSnapshotId = providerSnapshotId,
				searchTerm = searchTerm,
				RowspPage = pageSize,
                PageNumber = pageNumber,
                PageSize = pageSize,
            });
        }

        public async Task<IEnumerable<ProviderSummary>> GetProvidersInSnapshot(int providerSnapshotId, int pageNumber, int pageSize, string searchTerm)
        {
			searchTerm ??= "";
			searchTerm += "%";

			return await QuerySql<ProviderSummary>(@"SELECT @PageSize ProviderId, Name, Ukprn, ProviderType, ProviderSubType FROM Provider
WHERE ProviderSnapshotId = @ProviderSnapshotId and UkPrn like @searchTerm
ORDER BY Name
OFFSET ((@PageNumber - 1) * @RowspPage) ROWS
FETCH NEXT @RowspPage ROWS ONLY;
",
            new
            {
                providerSnapshotId = providerSnapshotId,
				searchTerm = searchTerm,
				RowspPage = pageSize,
                PageNumber = pageNumber,
                PageSize = pageSize,
            });
        }
    }
}