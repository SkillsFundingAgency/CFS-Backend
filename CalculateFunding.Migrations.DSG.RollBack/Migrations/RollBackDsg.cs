using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class RollBackDsg : IRollBackDsg
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly IDsgRollBackCosmosDocumentsJob _undoCosmosJob;

        public RollBackDsg(ICosmosRepository cosmosRepository,
            IDsgRollBackCosmosDocumentsJob undoCosmosJob)
        {
            _cosmosRepository = cosmosRepository;
            _undoCosmosJob = undoCosmosJob;
        }

        public async Task Run(MigrateOptions options)
        {
            try
            {
                Guard.ArgumentNotNull(options, nameof(options));
                
                await SetThroughPut(100000);
                await RollBackCosmosDocuments(options);
            }
            finally
            {
                await SetThroughPut(2000);
            }

            /*
             *      Due to incorrect configuration being set for the variation pointers in prod, the DSG specification has profiling incorrectly published.
             *      To correct the profiles in this version, we need to roll back to the previously published state as of March. To further complicate the data, a refresh has been performed which has incorrectly flattened the profiles in an updated state.
                    Create another console app in the backend solution to perform the migration, like CalculateFunding.Migrations.PublishedProviderPopulateReleased.
                    PublishedFunding
                    Rollback each of the DSG PublishedFunding documents in cosmos to the previous major version.
                    Replace c.content.current with the contents of the previous version based on c.content.current.majorVersion - 1. Lookup the PublishedFundingVersion based on the previous major version.
                    PublishedProvider
                    Rollback each of the DSG PublishedProvider documents in cosmos to the second latest released version, eg 
                    PublishedProvider.content.released.majorVersion - 1
                    (NOTE: see all versions with this query: where c.documentType='PublishedProviderVersion' and c.content.fundingStreamId = 'DSG' and c.content.status = 'Released' and c.content.providerId = '10006547')
                    eg for provider 10006547 the major version should be 3.0 as the current released version is publishedprovider-10006547-FY-2021-DSG-12
                    Replace c.contents.current and c.content.released in the PublishedProvider document with the contents of the PublishedProviderVersion above.
                    Testing and development
                    Copy the publishedfunding cosmos collection from prod into a new collection in the dev cosmos to perform development against using the dt.exe tool.
                    Once the documents have been reverted, we will need to run this against preprod to test with the current version of DSG and CFS software. It should override the blob contents and search indexes when republished for the already existing versions. A cache clear in the external API may be required.
                    
                    NOTE: the models for publishedproviders/published funding in the providers branch will be ahead of what's deployed into preprod/prod at the moment but should be backwards compatible.
                    1.	Order of deploying to an environment:
                    2.	Run the migration tool to revert DSG back to March
                    3.	Reindex published providers search
                    4.	Ensure provider totals are correct as per March
                    5.	Set variation pointer correctly
                    6.	Refresh funding
                    7.	Check profiling totals
                    8.	Approve funding
                    9.	Release funding
                    10.	Reindex published providers
                    11.	Clear external API cache key
                    12.	Ensure output for DSG is correct in external API

             * 
             */
        }

        private async Task RollBackCosmosDocuments(MigrateOptions migrateOptions)
        {
            await _undoCosmosJob.Run(migrateOptions);
        }

        private async Task SetThroughPut(int rus)
        {
            await _cosmosRepository.SetThroughput(rus);
        }
    }
}