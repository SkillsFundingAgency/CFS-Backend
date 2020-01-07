using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Etl.Extensions;
using CalculateFunding.Migrations.Specification.Etl.Migrations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specifications.Etl.Migrations
{
    public class SpecificationMigration
    {
        private CosmosDb _sourceDb;
        private CosmosDb _destinationDb;
        private Search _search;
        private IEnumerable<Container> _sourceContainers;
        private IEnumerable<(BlobContainer sourceContainer, BlobContainer destinationContainer)> _blobs;
        private string _specsContainerName;
        private string _specificationQuery;

        public SpecificationMigration(CosmosDb sourceDb, CosmosDb destinationDb, IEnumerable<Container> sourceContainers, IEnumerable<(BlobContainer sourceContainer, BlobContainer destinationContainer)> blobs, Search search, string specsContainerName, string specificationQuery)
        {
            Guard.ArgumentNotNull(sourceDb, nameof(sourceDb));
            Guard.ArgumentNotNull(destinationDb, nameof(destinationDb));
            Guard.ArgumentNotNull(sourceContainers, nameof(sourceContainers));
            Guard.ArgumentNotNull(blobs, nameof(blobs));
            Guard.ArgumentNotNull(search, nameof(search));
            Guard.ArgumentNotNull(specsContainerName, nameof(specsContainerName));
            Guard.ArgumentNotNull(specificationQuery, nameof(specificationQuery));

            _sourceDb = sourceDb;
            _destinationDb = destinationDb;
            _sourceContainers = sourceContainers;
            _blobs = blobs;
            _search = search;
            _specsContainerName = specsContainerName;
            _specificationQuery = specificationQuery;
        }

        public async Task Run(string specificationId)
        {
                if (await PreRequisites(specificationId))
                {
                    
                    // copy blob objects across
                    /*foreach (Container container in _sourceContainers)
                    {
                        int? currentThroughPut = null;

                        try
                        {
                            currentThroughPut = await _destinationDb.SetThroughPut(container.MaxThroughPut, container.Name);

                            container.Query = string.Format(container.Query, specificationId);
                            Console.WriteLine($"Started migrating specification '{specificationId}' documents from '{container.Name}' cosmos container");
                            string filename = @".\Tools\dt.exe";
                            string arguments = $"/ErrorLog:\"{container.Name}-errors.csv\" /OverwriteErrorLog:true /s:DocumentDB /s.ConnectionString:\"{_sourceDb.ConnectionString}\" /s.Collection:{container.Name} /s.Query:\"{container.Query}\" /t:DocumentDB /t.ConnectionString:\"{_destinationDb.ConnectionString}\" /t.Collection:{container.Name} /t.CollectionThroughput:{container.MaxThroughPut}" + (container.PartitionKey != null ? $" /t.PartitionKey:\"{container.PartitionKey}\"" : string.Empty);
                            Console.WriteLine(filename + " " + arguments);
                            var proc = new Process();
                            proc.StartInfo.FileName = filename;
                            proc.StartInfo.Arguments = arguments;
                            proc.Start();
                            await proc.WaitForExitAsync(TimeSpan.FromMinutes(10));
                            var exitCode = proc.ExitCode;
                            proc.Close();
                            Console.WriteLine($"Finished migrating specification '{specificationId}' documents from '{container.Name}' cosmos container");
                        }
                        finally
                        {
                            if(currentThroughPut.HasValue)
                            {
                                await _destinationDb.SetThroughPut(currentThroughPut.Value, container.Name, true);
                            }
                        }
                    }*/

                    // carry out post migration here
                    if (!await PostMigration(specificationId))
                    {
                        Console.Write("Post migration steps failure.");
                    }
                }
                else
                {
                    Console.Write("Pre-requisites failure.");
                }
            
        }

        private async Task<bool> PreRequisites(string specificationId)
        {
            try
            {
                IEnumerable<Task> allContainerBlobs = _blobs.Select(async (_) =>
                {
                    switch (_.sourceContainer.Name)
                    {
                        case "source":
                            {
                                Console.WriteLine($"Copying source blobs from source:{_.sourceContainer.AccountName} to destination:{_.destinationContainer.AccountName}");
                                StorageCredentials destStorageCredentials = new StorageCredentials(_.destinationContainer.AccountName, _.destinationContainer.AccountKey);
                                CloudStorageAccount destStorageAccount = new CloudStorageAccount(destStorageCredentials, "core.windows.net", true);
                                CloudBlobClient destBlobClient = destStorageAccount.CreateCloudBlobClient();
                                CloudBlobContainer destContainer = destBlobClient.GetContainerReference(_.destinationContainer.Name);

                                StorageCredentials sourceStorageCredentials = new StorageCredentials(_.sourceContainer.AccountName, _.sourceContainer.AccountKey);
                                CloudStorageAccount sourceStorageAccount = new CloudStorageAccount(sourceStorageCredentials, "core.windows.net", true);
                                CloudBlobClient sourceBlobClient = sourceStorageAccount.CreateCloudBlobClient();
                                CloudBlobContainer sourceContainer = sourceBlobClient.GetContainerReference(_.sourceContainer.Name);

                                IEnumerable<Task> containerBlobs = _.sourceContainer.Blobs.ToList().Select(async (blob) =>
                                {
                                    CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(blob);
                                    if (sourceBlob.Exists())
                                    {
                                        CloudBlockBlob destBlob = destContainer.GetBlockBlobReference(blob);

                                        var policy = new SharedAccessBlobPolicy
                                        {
                                            Permissions = SharedAccessBlobPermissions.Read,
                                            SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                                            SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7)
                                        };

                                        // Get SAS of that policy.
                                        var sourceBlobToken = sourceBlob.GetSharedAccessSignature(policy);

                                        // Make a full uri with the sas for the blob.
                                        var sourceBlobSAS = string.Format("{0}{1}", sourceBlob.Uri, sourceBlobToken);

                                        await destBlob.StartCopyAsync(new Uri(sourceBlobSAS));
                                    }
                                });

                                await Task.WhenAll(containerBlobs.ToArraySafe());

                                break;
                            }
                    }
                });

                await Task.WhenAll(allContainerBlobs);

                IEnumerable<Task<bool>> prereqs = _sourceContainers.Where(_ => _.HasPreReqs == true).ToList().Select(async (_) =>
                {
                    switch (_.Name)
                    {
                        case "publishedfunding":
                            {
                                dynamic item = await GetSpecification(_destinationDb, specificationId);

                                if (item == null)
                                {
                                    return false;
                                }

                                string fundingPeriodId = item.content.current.fundingPeriod.Id;
                                string providerVersionId = item.content.current.providerVersionId.Id;

                                item = await _sourceDb.GetDocument(new CosmosDbQuery
                                {
                                    QueryText = @"SELECT * FROM c
                                        WHERE c.documentType = 'FundingPeriod'
                                        AND c.content.id = @fundingPeriodId",
                                    Parameters = new[]
                                         {
                                        new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                                         }
                                },
                                "policy");

                                if (item == null)
                                {
                                    return false;
                                }

                                item = await _sourceDb.GetDocument(new CosmosDbQuery
                                {
                                    QueryText = @"SELECT * FROM c
                                        WHERE c.documentType = 'ProviderVersionMetadata'
                                        AND c.content.id = @providerVersionId",
                                    Parameters = new[]
                                    {
                                        new CosmosDbQueryParameter("@providerVersionId", providerVersionId)
                                    }
                                },
                                "providerversionsmetadata");

                                if (item == null)
                                {
                                    return false;
                                }

                                break;
                            }
                        case "datasets":
                            {
                                dynamic item = await _sourceDb.GetDocument(new CosmosDbQuery
                                {
                                    QueryText = @"SELECT * FROM c
                                        WHERE c.documentType = 'DefinitionSpecificationRelationship'
                                        AND c.content.Specification.id = @specificationId",
                                    Parameters = new[]
                                         {
                                    new CosmosDbQueryParameter("@specificationId", specificationId)
                                    }
                                },
                                _.Name);

                                if (item == null)
                                {
                                    // no definitions
                                    return true;
                                }
                                else
                                {
                                    string datasetVersionId = item.content.DatasetVersion.Id;
                                    string datasetDefinitionId = item.content.DatasetDefinition.id;
                                    _.Query = string.Join(" ", _.Query, $"OR c.id = '{datasetVersionId}'");

                                    item = await _sourceDb.GetDocument(new CosmosDbQuery
                                    {
                                        QueryText = @"SELECT * FROM c
                                        WHERE c.documentType = 'DatasetDefinition'
                                        AND c.id = @datasetDefinitionId",
                                        Parameters = new[]
                                        {
                                        new CosmosDbQueryParameter("@datasetDefinitionId", datasetDefinitionId)
                                        }
                                    },
                                    _.Name);

                                    if (item == null)
                                    {
                                        return false;
                                    }
                                }

                                break;
                            }
                    }

                    return true;
                });

                return (await Task.WhenAll(prereqs.ToArraySafe())).All(_ => _ == true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private async Task<bool> PostMigration(string specificationId)
        {
            SearchRepositorySettings searchRepositorySettings = new SearchRepositorySettings { SearchServiceName = _search.SearchServiceName, SearchKey = _search.SearchKey };

            try
            {
                IEnumerable<Task<bool>> poststeps = _sourceContainers.Where(_ => _.HasPost == true).ToList().Select(async (_) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings { ConnectionString = _destinationDb.ConnectionString, DatabaseName = _destinationDb.DatabaseName };

                    switch (_.Name)
                    {
                        case "calcs":
                            {
                                Console.WriteLine($"Started re-indexing :{_.Name}");

                                dynamic item = await GetSpecification(_destinationDb, specificationId);

                                if (item == null)
                                {
                                    return false;
                                }

                                SearchRepository<CalculationIndex> searchRepository = new SearchRepository<CalculationIndex>(searchRepositorySettings);

                                IEnumerable<dynamic> items = await _destinationDb.GetDocuments(new CosmosDbQuery
                                {
                                    QueryText = string.Format(_.Query, specificationId)
                                },
                                _.Name);

                                if (items == null || !items.Any())
                                {
                                    Console.Write($"Completed re-indexing:{_.Name} no results found");
                                    return true;
                                }

                                IEnumerable<CalculationIndex> calculations = items.Where(calc => calc.documentType == "Calculation").Select(calc =>
                                {
                                    return new CalculationIndex
                                    {
                                        Id = calc.id,
                                        SpecificationId = calc.content.specificationId,
                                        SpecificationName = item.content.name,
                                        Name = calc.content.current.name,
                                        ValueType = calc.content.current.valueType.ToString(),
                                        FundingStreamId = string.IsNullOrWhiteSpace(Convert.ToString(calc.content.fundingStreamId)) ? "N/A" : calc.content.fundingStreamId,
                                        FundingStreamName = (((IEnumerable<dynamic>)item.content.current.fundingStreams)?.FirstOrDefault()?.name) ?? "N/A",
                                        Namespace = calc.content.current.@namespace?.ToString(),
                                        CalculationType = calc.content.current.calculationType.ToString(),
                                        Description = calc.content.current.description,
                                        WasTemplateCalculation = calc.content.current.wasTemplateCalculation,
                                        Status = calc.content.current.publishStatus.ToString(),
                                        LastUpdatedDate = DateTimeOffset.Now
                                    };
                                });

                                Console.WriteLine($"Number of {_.Name} to index:{calculations.Count()}");

                                await searchRepository.Index(calculations);

                                Console.WriteLine($"Completed re-indexing :{_.Name}");

                                break;
                            }
                        case "calculationresults":
                            {
                                Console.WriteLine($"Started re-indexing :{_.Name}");

                                dynamic item = await GetSpecification(_destinationDb, specificationId);

                                if (item == null)
                                {
                                    return false;
                                }

                                SearchRepository<ProviderCalculationResultsIndex> searchRepository = new SearchRepository<ProviderCalculationResultsIndex>(searchRepositorySettings);

                                IEnumerable<dynamic> items = await _destinationDb.GetDocuments(new CosmosDbQuery
                                {
                                    QueryText = string.Format(_.Query, specificationId)
                                },
                                _.Name);

                                if (items == null || !items.Any())
                                {
                                    Console.WriteLine($"Completed re-indexing:{_.Name} no results found");
                                    return true;
                                }

                                IEnumerable<ProviderCalculationResultsIndex> providerResults = items.Select(providerResult =>
                                {
                                    return new ProviderCalculationResultsIndex
                                    {
                                        SpecificationId = providerResult.content.specificationId,
                                        SpecificationName = item.content.name,
                                        ProviderId = providerResult.content.provider?.id,
                                        ProviderName = providerResult.content.provider?.name,
                                        ProviderType = providerResult.content.provider?.providerType,
                                        ProviderSubType = providerResult.content.provider?.providerSubType,
                                        LocalAuthority = providerResult.content.provider?.Authority,
                                        LastUpdatedDate = providerResult.content.createdAt,
                                        UKPRN = providerResult.content.provider?.ukPrn,
                                        URN = providerResult.content.provider?.urn,
                                        UPIN = providerResult.content.provider?.upin,
                                        EstablishmentNumber = providerResult.content.provider?.establishmentNumber,
                                        OpenDate = !string.IsNullOrWhiteSpace(Convert.ToString(providerResult.content.provider.dateOpened)) ? DateTimeOffset.Parse(Convert.ToString(providerResult.content.provider.dateOpened)) : null,
                                        CalculationId = ((IEnumerable<dynamic>)providerResult.content.calcResults).Select(m => Convert.ToString(m.calculation.id)).Cast<string>().ToArraySafe(),
                                        CalculationName = ((IEnumerable<dynamic>)providerResult.content.calcResults).Select(m => Convert.ToString(m.calculation.name)).Cast<string>().ToArraySafe(),
                                        CalculationResult = ((IEnumerable<dynamic>)providerResult.content.calcResults).Select(m => Convert.ToString(m.value) ?? "null").Cast<string>().ToArraySafe(),
                                        CalculationException = ((IEnumerable<dynamic>)providerResult.content.calcResults).Where(m => !string.IsNullOrWhiteSpace(Convert.ToString(m.exceptionType))).Select(m => Convert.ToString(m.calculation.id)).Cast<string>().ToArraySafe(),
                                        CalculationExceptionType = ((IEnumerable<dynamic>)providerResult.content.calcResults).Select(m => Convert.ToString(m.exceptionType) ?? string.Empty).Cast<string>().ToArraySafe(),
                                        CalculationExceptionMessage = ((IEnumerable<dynamic>)providerResult.content.calcResults).Select(m => Convert.ToString(m.exceptionMessage) ?? string.Empty).Cast<string>().ToArraySafe()
                                    };
                                });

                                Console.WriteLine($"Number of {_.Name} to index:{providerResults.Count()}");

                                await searchRepository.Index(providerResults);

                                Console.WriteLine($"Completed re-indexing :{_.Name}");

                                break;
                            }
                        case "specs":
                            {
                                cosmosDbSettings.ContainerName = _specsContainerName;

                                dynamic item = await GetSpecification(_destinationDb, specificationId);

                                if (item == null)
                                {
                                    return false;
                                }

                                SearchRepository<SpecificationIndex> searchRepository = new SearchRepository<SpecificationIndex>(searchRepositorySettings);

                                SpecificationIndex spec = new SpecificationIndex
                                {
                                    Id = item.id,
                                    Name = item.content.current.name,
                                    FundingStreamIds = ((IEnumerable<dynamic>)item.content.current.fundingStreams).Select(s => Convert.ToString(s.id)).Cast<string>().ToArray(),
                                    FundingStreamNames = ((IEnumerable<dynamic>)item.content.current.fundingStreams).Select(s => Convert.ToString(s.name)).Cast<string>().ToArray(),
                                    FundingPeriodId = item.content.current.fundingPeriod.id,
                                    FundingPeriodName = item.content.current.fundingPeriod.name,
                                    LastUpdatedDate = !string.IsNullOrWhiteSpace(Convert.ToString(item.updatedAt)) ? DateTimeOffset.Parse(Convert.ToString(item.updatedAt)) : null,
                                    Description = item.content.current.description,
                                    Status = item.content.current.publishStatus
                                };

                                await searchRepository.Index(new[] { spec });

                                break;
                            }
                        case "publishedfunding":
                            {
                                Console.WriteLine($"Started re-indexing : published provider results");

                                IEnumerable<dynamic> items = await _destinationDb.GetDocuments(new CosmosDbQuery
                                {
                                    QueryText = string.Format(_.Query, specificationId)
                                },
                                _.Name);

                                if (items == null || !items.Any())
                                {
                                    Console.WriteLine($"Completed re-indexing: published provider results no results found");
                                    return true;
                                }

                                SearchRepository<PublishedProviderIndex> searchRepository = new SearchRepository<PublishedProviderIndex>(searchRepositorySettings);

                                IEnumerable<PublishedProviderIndex> providers = items.Where(provider => provider.documentType == "PublishedProvider").Select(provider =>
                                {
                                    return new PublishedProviderIndex
                                    {
                                        Id = $"{provider.content.current.providerId}-{provider.content.current.fundingPeriodId}-{provider.content.current.fundingStreamId}",
                                        ProviderType = provider.content.current.provider.ProviderType,
                                        LocalAuthority = provider.content.current.provider.LocalAuthorityName,
                                        FundingStatus = provider.content.current.status.ToString(),
                                        ProviderName = provider.content.current.provider.name,
                                        UKPRN = provider.content.current.provider.ukPrn,
                                        FundingValue = Convert.ToDouble(provider.content.current.totalFunding),
                                        SpecificationId = provider.content.current.specificationId,
                                        FundingStreamId = provider.content.current.fundingStreamId,
                                        FundingPeriodId = provider.content.current.fundingPeriodId,
                                    };
                                });

                                Console.WriteLine($"Number of published provider results to index:{providers.Count()}");

                                await searchRepository.Index(providers);

                                Console.WriteLine($"Completed re-indexing : published provider results");

                                SearchRepository<PublishedFundingIndex> searchFundingRepository = new SearchRepository<PublishedFundingIndex>(searchRepositorySettings);

                                Console.WriteLine($"Started running indexer : published funding results");

                                await searchFundingRepository.RunIndexer();

                                break;
                            }
                    }

                    return true;
                });

                return (await Task.WhenAll(poststeps.ToArraySafe())).All(_ => _ == true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private async Task<dynamic> GetSpecification(CosmosDb cosmosDb, string specificationId)
        {
            return await cosmosDb.GetDocument(new CosmosDbQuery
            {
                QueryText = _specificationQuery,
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId)
                }
            },
            _specsContainerName);
        }
    }
}
