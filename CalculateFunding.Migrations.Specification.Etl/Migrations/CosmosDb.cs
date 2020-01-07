using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Etl.Migrations
{
    public class CosmosDb
    {
        public string ConnectionString => $"AccountEndpoint={_endpoint};AccountKey={_accesskey};Database={_database};";

        public string DatabaseName => _database;

        private readonly CosmosDbSettings _cosmosDbSettings;

        private readonly string _accesskey;

        private readonly string _endpoint;

        public readonly string _database;

        public CosmosDb(string accesskey, string endpoint, string database)
        {
            Guard.IsNullOrWhiteSpace(accesskey, nameof(accesskey));
            Guard.IsNullOrWhiteSpace(endpoint, nameof(endpoint));
            Guard.IsNullOrWhiteSpace(database, nameof(database));

            _accesskey = accesskey;
            _endpoint = endpoint;
            _database = database;
            _cosmosDbSettings = new CosmosDbSettings { ConnectionString = ConnectionString, DatabaseName = DatabaseName };
        }

        internal async Task<dynamic> GetDocument(CosmosDbQuery cosmosDbQuery, string containerName)
        {
            return (await GetDocuments(cosmosDbQuery, containerName)).FirstOrDefault();
        }

        internal async Task<IEnumerable<dynamic>> GetDocuments(CosmosDbQuery cosmosDbQuery, string containerName)
        {
            _cosmosDbSettings.ContainerName = containerName;

            ICosmosRepository cosmosRepository = new CosmosRepository(_cosmosDbSettings);

            IEnumerable<dynamic> queryResults = await cosmosRepository
                 .DynamicQuery(cosmosDbQuery);

            return queryResults;
        }

        internal async Task<int?> SetThroughPut(int requestUnits, string containerName, bool force = false)
        {
            _cosmosDbSettings.ContainerName = containerName;

            ICosmosRepository cosmosRepository = new CosmosRepository(_cosmosDbSettings);

            await cosmosRepository.EnsureContainerExists();

            int? currentRequestUnits = await cosmosRepository.GetThroughput();

            Console.WriteLine($"Container Name:{containerName} Throughput: Current:{currentRequestUnits} New:{requestUnits} Force:{force}");

            if (currentRequestUnits < requestUnits || force)
            {
                await cosmosRepository.SetThroughput(requestUnits);
            }

            return currentRequestUnits;
        }
    }
}
