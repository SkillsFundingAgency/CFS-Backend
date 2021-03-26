using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Interfaces;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Polly;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class DatasetCloneBuilderFactory : IDatasetCloneBuilderFactory
    {
        private readonly IBlobClient _blobs;
        private readonly IDatasetsResiliencePolicies _resiliencePolicies;

        public DatasetCloneBuilderFactory(IBlobClient blobs,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(blobs, nameof(blobs));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            
            _blobs = blobs;
            _resiliencePolicies = resiliencePolicies;
        }

        public IDatasetCloneBuilder CreateCloneBuilder() =>
            new DatasetCloneBuilder(_blobs,
                _resiliencePolicies);
    }
    
    /// <summary>
    /// Ensure this is registered as a scoped instance, rather than singleton in IoC
    /// </summary>
    public class DatasetCloneBuilder : IDatasetCloneBuilder
    {
        public DatasetCloneBuilder(IBlobClient blobs,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Blobs = blobs;
            BlobsResilience = resiliencePolicies.BlobClient;
        }

        public IBlobClient Blobs { get; }

        public AsyncPolicy BlobsResilience { get; }

        public Task<RowCopyResult> CopyRow(string sourceProviderId, string destinationProviderId)
        {
            // Copy a row if it exists, between the source and destination provider
            // Return a result based on if the row was copied or not.
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetExistingIdentifierValues(string fieldNameOfIdentifier)
        {
            // Get all existing values from the identifier column in the first sheet of the loaded spreadsheet
            throw new NotImplementedException();
        }

        public async Task LoadOriginalDataset(Dataset dataset)
        {
            // Load excel file from blob storage, load data and store within a class variable
            throw new NotImplementedException();
        }

        public Task<DatasetVersion> SaveContents(Reference author)
        {
            //TODO; change this to a query method to build the data set for us to save externally
            
            // Save contents as a new dataset version - use existing service to save a new version
            throw new NotImplementedException();
        }
    }
}
