using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalculateFunding.Services.DataImporter
{
    public class AptBasicEntitlement : ProviderSourceDataset
    {
        [Description("Primary Amount Per Pupil")]
        [JsonProperty("primaryAmountPerPupil")]
        public decimal PrimaryAmountPerPupil { get; set; }

        /// <summary>
        /// This is the primary amount
        /// </summary>
        [Description("Primary Amount")]
        [JsonProperty("primaryAmount")]
        public decimal PrimaryAmount { get; set; }


    }

    public class CensusNumberCounts : ProviderSourceDataset
    {
        [Description("NOR Primary")]
        [JsonProperty("norPrimary")]
        public int NORPrimary { get; set; }

    }

    public class AptLocalAuthority : ProviderSourceDataset
    {
        [Description("Primary Notional SEN")]
        [JsonProperty("primaryNotionalSEN")]
        public decimal PrimaryNotionalSEN { get; set; }

    }

    public class AptProviderInformation : ProviderSourceDataset
    {
        [JsonProperty("UPin")]
        public string UPIN { get; set; }
        [JsonProperty("dateOpened")]
        public DateTimeOffset DateOpened { get; set; }
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }
        [JsonProperty("phase")]
        public string Phase { get; set; }

    }


    public class DataImporterService
    {
        private readonly CosmosRepository _datasetRespository;
        private readonly ILogger _logger;

        public DataImporterService(CosmosRepository datasetRespository, ILogger<DataImporterService> logger)
        {
            _datasetRespository = datasetRespository;
            _logger = logger;
        }


        public async Task GetSourceDataAsync(string name, Stream stream, string budgetId)
        {
            _logger.LogInformation(name);
            var reader = new ExcelReader();


                switch (name.ToLowerInvariant())
                {
                    case "export apt.xlsx":
                        var aptSourceRecords =
                            reader.Read<AptSourceRecord>(stream).ToArray();

                        foreach (var aptSourceRecord in aptSourceRecords)
                        {
                            var providerInformation = new AptProviderInformation
                            {
                                BudgetId = budgetId,
                                DatasetName = "APT Provider Information",
                                ProviderUrn = aptSourceRecord.URN,
                                DateOpened = aptSourceRecord.DateOpened,
                                LocalAuthority = aptSourceRecord.LocalAuthority,
                                ProviderName = aptSourceRecord.ProviderName,
                                UPIN = aptSourceRecord.UPIN,
                                Phase = aptSourceRecord.Phase
                                
                            };
                            await _datasetRespository.CreateAsync(providerInformation);

                            var basicEntitlement = new AptBasicEntitlement
                            {
                                BudgetId = budgetId,
                                DatasetName = "APT Basic Entitlement",
                                ProviderUrn = aptSourceRecord.URN,
                                ProviderName = aptSourceRecord.ProviderName,
                                PrimaryAmount = aptSourceRecord.PrimaryAmount,
                                PrimaryAmountPerPupil = aptSourceRecord.PrimaryAmountPerPupil


                            };
                            await _datasetRespository.CreateAsync(basicEntitlement);

                        }
                        break;
                    case "number counts export.xlsx":
                        var numberCountSourceRecords =
                            reader.Read<NumberCountSourceRecord>(stream).ToArray();


                        foreach (var numberCountSourceRecord in numberCountSourceRecords)
                        {
                            var censusNumberCount = new CensusNumberCounts
                            {
                                BudgetId = budgetId,
                                DatasetName = "Census Number Counts",
                                ProviderUrn = numberCountSourceRecord.URN,
                                ProviderName = numberCountSourceRecord.ProviderName,
                                NORPrimary = numberCountSourceRecord.NumberOnRollPrimary


                            };
                            await _datasetRespository.CreateAsync(censusNumberCount);

                        }
                        break;
                    case "export apt la mapped.xlsx":
                        var aptLocalAuthorityRecords =
                            reader.Read<AptLocalAuthorityRecord>(stream).ToArray();


                        foreach (var aptLocalAuthorityRecord in aptLocalAuthorityRecords)
                        {
                            var censusNumberCount = new AptLocalAuthority
                            {
                                BudgetId = budgetId,
                                DatasetName = "APT Local Authority",
                                ProviderUrn = aptLocalAuthorityRecord.URN,
                                ProviderName = aptLocalAuthorityRecord.ProviderName,
                                PrimaryNotionalSEN = aptLocalAuthorityRecord.PrimaryNotionalSEN


                            };
                            await _datasetRespository.CreateAsync(censusNumberCount);

                        }
                        break;
                    default:
                        break;
                }



        }
    }
}


    
