namespace CalculateFunding.Profiling.ConsoleConfig.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Dtos;
	using Extensions;
	using Microsoft.Azure.Documents;
	using Microsoft.Azure.Documents.Client;
	using Microsoft.Extensions.Configuration;

	public static class ProfilePatternRepository
    {
        private const string DatabaseId = "calculate-funding";
        private const string CollectionId = "profiling";

        private static DocumentClient _documentClient;

        public static void InitialiseDocumentClient(CosmosDbSettings cosmosDbSettings)
        {
	        _documentClient = CosmosHelper.Parse(cosmosDbSettings.ConnectionString);
		}

        public static async Task UpsertPattern(FundingStreamPeriodProfilePatternDocument patternDocument)
        {
            InitialiseDocumentClientIfNull();

            Console.WriteLine($"Upserting pattern for FSP: {patternDocument.FundingStreamPeriodCode}");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);

            await _documentClient.UpsertDocumentAsync(collectionUri, patternDocument);
        }

        public static async Task RemoveWhereNoIdMatch(IEnumerable<string> knownDocumentIds)
        {
            InitialiseDocumentClientIfNull();

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);

            FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true };

            string sqlQueryIds = string.Join(",", knownDocumentIds.Select(d => $"'{d}'"));

            IQueryable<dynamic> query = _documentClient.CreateDocumentQuery(
                collectionUri,
                $"SELECT pattern.id FROM pattern WHERE pattern.id NOT IN ({sqlQueryIds})",
                options);

            await query.ToList()
                .ForEachAsync(async qr =>
                {
                    Uri documentUri = UriFactory.CreateDocumentUri(DatabaseId, CollectionId, qr.id);
                    await _documentClient.DeleteDocumentAsync(documentUri);
                });
        }

        public static async Task CreateDatabaseAndCollectionIfNotExists()
        {
            InitialiseDocumentClientIfNull();
            await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });

            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                new DocumentCollection {Id = CollectionId});
        }

        private static void InitialiseDocumentClientIfNull()
        {
            if (_documentClient == null)
            {
                InitialiseDocumentClient(null);
            }
        }
    }
}