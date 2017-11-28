using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Allocations.Models;
using CalculateFunding.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using Newtonsoft.Json;

namespace Allocations.Repository
{
    public class SearchInitializer
    {
        private readonly SearchServiceClient _searchServiceClient;
        private static readonly Dictionary<Type, DataType> TypeMapping = new Dictionary<Type, DataType> {
            { typeof(int), DataType.Int32 },
            { typeof(int?), DataType.Int32 },
            { typeof(long), DataType.Int64 },
            { typeof(long?), DataType.Int64 },
            { typeof(decimal), DataType.Double },
            { typeof(decimal?), DataType.Double },
            { typeof(double), DataType.Double },
            { typeof(double?), DataType.Double },
            { typeof(DateTime), DataType.DateTimeOffset },
            { typeof(DateTime?), DataType.DateTimeOffset },
            { typeof(DateTimeOffset), DataType.DateTimeOffset },
            { typeof(DateTimeOffset?), DataType.DateTimeOffset },
            { typeof(bool), DataType.Boolean },
            { typeof(bool?), DataType.Boolean },
            { typeof(string), DataType.String },
            { typeof(GeographyPoint), DataType.GeographyPoint },
        };

        private readonly string _documentDbConnectionString;

        public SearchInitializer(string searchServiceName, string searchServicePrimaryKey, string documentDbConnectionString)
        {
            _searchServiceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServicePrimaryKey));
            _documentDbConnectionString = documentDbConnectionString;
        }



        public async Task Initialise(params Type[] types)
        {
            foreach (var type in types)
            {
                var definition = GetDefinition(type);

                await CreateIndexAsync(definition);

                var attribute = type.GetCustomAttribute<SearchIndexAttribute>();
                if (attribute?.IndexerForType != null)
                {
                    await InitialiseIndexer(type, attribute);
                }
            }



        }

        private async Task CreateIndexAsync(Index definition)
        {
            try
            {
                Console.WriteLine($"Creating Index '${definition.Name}'");
                var index = await _searchServiceClient.Indexes.CreateOrUpdateAsync(definition);
                Console.WriteLine($"Created Index '${index.Name}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Creating Index '{definition.Name}'");
            }
        }

        public async Task InitialiseIndexer(Type indexType, SearchIndexAttribute attribute)
        {
           var dataSourceDefinition = new DataSource
            {
                Name = attribute.IndexerForType.Name.ToLowerInvariant(),
                Type = DataSourceType.DocumentDb,
                Credentials = new DataSourceCredentials($"{_documentDbConnectionString}Database={attribute.DatabaseName}"),
                Container = new DataContainer { Name = attribute.CollectionName, Query = attribute.IndexerQuery },
                DataChangeDetectionPolicy =
                    new HighWaterMarkChangeDetectionPolicy { HighWaterMarkColumnName = "_ts" },
                DataDeletionDetectionPolicy =
                    new SoftDeleteColumnDeletionDetectionPolicy
                    {
                        SoftDeleteColumnName = "deleted",
                        SoftDeleteMarkerValue = "true"
                    },
               

            };
            try
            {
                Console.WriteLine($"Creating Search DataSource '${dataSourceDefinition.Name}'");
                var dataSource = await _searchServiceClient.DataSources.CreateOrUpdateAsync(dataSourceDefinition);
                Console.WriteLine($"Created Search DataSource '${dataSource.Name}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Creating Search DataSource '{dataSourceDefinition.Name}'");
            }


            var definition = new Indexer
            {
                Name = $"{attribute.IndexerForType.Name}-{indexType.Name}".ToLowerInvariant(),
                DataSourceName = dataSourceDefinition.Name.ToLowerInvariant(),
                TargetIndexName = indexType.Name.ToLowerInvariant(),
                Schedule = new IndexingSchedule { Interval = TimeSpan.FromMinutes(5), },
                Parameters = new IndexingParameters { }

            };

            try
            {
                Console.WriteLine($"Creating Indexer '${definition.Name}'");
                var indexer = await _searchServiceClient.Indexers.CreateOrUpdateAsync(definition);
                Console.WriteLine($"Created Indexer '${indexer.Name}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Creating Indexer '{definition.Name}'");
            }
        }


        private static Index GetDefinition(Type type)
        {
            var properties = type.GetProperties().OrderBy(x => x.MetadataToken).ToArray();
            var fields = new List<Field>();
            foreach (var propertyInfo in properties)
            {
                var attributes = propertyInfo.CustomAttributes.ToArray();
                if (attributes.All(x => x.AttributeType != typeof(JsonIgnoreAttribute)))
                {
                    DataType dataType;

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        if (!TypeMapping.TryGetValue(propertyInfo.PropertyType.GetElementType(), out var arrayElementType))
                            throw new ArgumentException($"{propertyInfo.PropertyType.Name} is not supported");
                        dataType = DataType.Collection(arrayElementType);
                    }
                    else
                    {
                        if (!TypeMapping.TryGetValue(propertyInfo.PropertyType, out dataType))
                            throw new ArgumentException($"{propertyInfo.PropertyType.Name} is not supported");
                    }

                    var jsonProperty = propertyInfo.GetCustomAttribute<JsonPropertyAttribute>();

                    var field = new Field
                    {
                        Name = jsonProperty != null ? jsonProperty.PropertyName : propertyInfo.Name,
                        Type = dataType,
                        IsKey = attributes.Any(x => x.AttributeType.Name == "KeyAttribute"),
                        IsSearchable = attributes.Any(x => x.AttributeType == typeof(IsSearchableAttribute)),
                        IsFilterable = attributes.Any(x => x.AttributeType == typeof(IsFilterableAttribute)),
                        IsFacetable = attributes.Any(x => x.AttributeType == typeof(IsFacetableAttribute)),

                    };
                    fields.Add(field);
                }
            }

            return new Index()
            {
                Name = type.Name.ToLowerInvariant(),
                Fields = fields.ToArray()
            };
        }
    }
}