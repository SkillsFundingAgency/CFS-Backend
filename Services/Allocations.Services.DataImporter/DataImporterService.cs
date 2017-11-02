using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Datasets;
using Allocations.Repository;
using Newtonsoft.Json;

namespace Allocations.Services.DataImporter
{
    [Dataset("budget-gag1718", "APT Basic Entitlement")]
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

        [Description("Primary Notional SEN")]
        [JsonProperty("primaryNotionalSEN")]
        public decimal PrimaryNotionalSEN { get; set; }
    }

    [Dataset("budget-gag1718", "Census Number Counts")]
    public class CensusNumberCounts : ProviderSourceDataset
    {
        [Description("NOR Primary")]
        [JsonProperty("norPrimary")]
        public int NORPrimary { get; set; }

    }

    [Dataset("budget-gag1718", "APT Provider Information")]
    public class AptProviderInformation : ProviderSourceDataset
    {
        [JsonProperty("UPin")]
        public string UPIN { get; set; }
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }
        [JsonProperty("dateOpened")]
        public DateTimeOffset DateOpened { get; set; }
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

    }

    public class DataImporterService
    {

        public async Task GetSourceDataAsync()
        {
            var reader = new ExcelReader();

            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {
                var aptSourceRecords =
                    reader.Read<AptSourceRecord>(@"SourceData\Export APT.XLSX").ToArray();

                var numberCountSourceRecords =
                    reader.Read<NumberCountSourceRecord>(@"SourceData\Number Counts Export.XLSX").ToArray();

                foreach (var aptSourceRecord in aptSourceRecords)
                {
                    var providerInformation = new AptProviderInformation
                    {
                        BudgetId = typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        ProviderUrn = aptSourceRecord.URN,
                        DateOpened = aptSourceRecord.DateOpened,
                        LocalAuthority = aptSourceRecord.LocalAuthority,
                        ProviderName = aptSourceRecord.ProviderName,
                        UPIN = aptSourceRecord.UPIN
                    };
                    await repository.CreateAsync(providerInformation);

                    var basicEntitlement = new AptBasicEntitlement
                    {
                        BudgetId = typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        ProviderUrn = aptSourceRecord.URN,

                        PrimaryAmount = aptSourceRecord.PrimaryAmount,
                        PrimaryAmountPerPupil = aptSourceRecord.PrimaryAmountPerPupil,
                        PrimaryNotionalSEN = aptSourceRecord.PrimaryNotionalSEN


                    };
                    await repository.UpsertAsync(basicEntitlement);

                }
                foreach (var numberCountSourceRecord in numberCountSourceRecords)
                {
                    var censusNumberCount = new CensusNumberCounts
                    {
                        BudgetId = typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        ProviderUrn = numberCountSourceRecord.URN,

                        NORPrimary = numberCountSourceRecord.NumberOnRollPrimary


                    };
                    await repository.UpsertAsync(censusNumberCount);

                }
            }


        }

        public async Task GetSourceDataAsync(string name, Stream stream)
        {
            var reader = new ExcelReader();


            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {



                switch (name.ToLowerInvariant())
                {
                    case "export apt.xlsx":
                        var aptSourceRecords =
                            reader.Read<AptSourceRecord>(stream).ToArray();

                        foreach (var aptSourceRecord in aptSourceRecords)
                        {
                            var providerInformation = new AptProviderInformation
                            {
                                BudgetId = typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>()
                                    .ModelName,
                                DatasetName =
                                    typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>().DatasetName,
                                ProviderUrn = aptSourceRecord.URN,
                                DateOpened = aptSourceRecord.DateOpened,
                                LocalAuthority = aptSourceRecord.LocalAuthority,
                                ProviderName = aptSourceRecord.ProviderName,
                                UPIN = aptSourceRecord.UPIN
                            };
                            await repository.CreateAsync(providerInformation);

                            var basicEntitlement = new AptBasicEntitlement
                            {
                                BudgetId = typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().ModelName,
                                DatasetName =
                                    typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().DatasetName,
                                ProviderUrn = aptSourceRecord.URN,

                                PrimaryAmount = aptSourceRecord.PrimaryAmount,
                                PrimaryAmountPerPupil = aptSourceRecord.PrimaryAmountPerPupil,
                                PrimaryNotionalSEN = aptSourceRecord.PrimaryNotionalSEN


                            };
                            await repository.CreateAsync(basicEntitlement);

                        }
                        break;
                    case "number counts export.xlsx":
                        var numberCountSourceRecords =
                            reader.Read<NumberCountSourceRecord>(stream).ToArray();


                        foreach (var numberCountSourceRecord in numberCountSourceRecords)
                        {
                            var censusNumberCount = new CensusNumberCounts
                            {
                                BudgetId = typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().ModelName,
                                DatasetName =
                                    typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().DatasetName,
                                ProviderUrn = numberCountSourceRecord.URN,

                                NORPrimary = numberCountSourceRecord.NumberOnRollPrimary


                            };
                            await repository.CreateAsync(censusNumberCount);

                        }
                        break;
                    default:
                        break;
                }


            }


        }
    }
}


    
