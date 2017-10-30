using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Allocations.Repository;
using AY1718.CSharp.Datasets;

namespace Allocations.Services.DataImporter
{

    public class DataImporterService
    {

        public async Task GetSourceDataAsync()
        {
            var reader = new ExcelReader();
            var databaseName = ConfigurationManager.AppSettings["DocumentDB.DatabaseName"];
            var endpoint = new Uri(ConfigurationManager.AppSettings["DocumentDB.Endpoint"]);
            var key = ConfigurationManager.AppSettings["DocumentDB.Key"];

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



                switch (name)
                {
                    case "Export APT.XLSX":
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
                            await repository.UpsertAsync(basicEntitlement);

                        }
                        break;
                    case "Number Counts Export.XLSX":
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
                            await repository.UpsertAsync(censusNumberCount);

                        }
                        break;
                    default:
                        break;
                }


            }


        }
    }
}


    
