using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Academies.AY1718.Datasets;
using Allocations.Models.Framework;
using Allocations.Respository;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions;

namespace EndToEndDemo
{
    public class SourceColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public SourceColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
    public class AptSourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Provider Information.UPIN_9068")]
        public string UPIN { get; set; }
        [SourceColumn("Provider Information.Provider Name_9070")]
        public string ProviderName { get; set; }
        [SourceColumn("Provider Information.Date Opened_9077")]
        public DateTimeOffset DateOpened { get; set; }
        [SourceColumn("Provider Information.Local Authority_9426")]
        public string LocalAuthority { get; set; }

        [SourceColumn("APT New ISB dataset.Basic Entitlement Primary_71855")]
        public decimal PrimaryAmount { get; set; }
        [SourceColumn("APT New ISB dataset.15-16 Post MFG per pupil Budget_71961")]
        public decimal PrimaryAmountPerPupil { get; set; }
        [SourceColumn("APT New ISB dataset.Notional SEN Budget_71939")]
        public decimal PrimaryNotionalSEN { get; set; }

        //[SourceColumn("APT Inputs and Adjustments.NOR_71991")]
        //public decimal NumberOnRoll { get; set; }

        //[SourceColumn("APT Inputs and Adjustments.NOR Primary_71993")]
        //public decimal NumberOnRollPrimary { get; set; }


    }


    public class NumberCountSourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Census Number Counts.NOR_70999")]
        public int NumberOnRoll { get; set; }
        [SourceColumn("Census Number Counts.NOR Primary_71001")]
        public int NumberOnRollPrimary { get; set; }
 

    }

    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                await GetSourceDataAsync();

                await GenerateAllocations();
            }

            // Do any async anything you need here without worry
        ). GetAwaiter().GetResult();
    }

    private static async Task GenerateAllocations()
        {
            using (var repository = new Repository("datasets"))
            {
                var modelName = "SBS1718";
                var urns = repository.GetProviderUrns(modelName);

                foreach (var urn in urns)
                {
                    var datasets = repository.GetProviderDatasets(modelName, urn).ToArray();

                    var typedDatasets = new List<object>();
                    foreach (var dataset in datasets)
                    {
                        var type = AllocationFactory.GetDatasetType(dataset.DatasetName);


                        object blah = JsonConvert.DeserializeObject(dataset.Json, type);
                        typedDatasets.Add(blah);
                    }

                    var model =
                        AllocationFactory.CreateAllocationModel(modelName);

                    var providerAllocations = model.Execute(modelName, urn, typedDatasets.ToArray());
                    using (var allocationRepository = new Repository("allocations"))
                    {
                        foreach (var providerAllocation in providerAllocations)
                        {
                            await allocationRepository.UpsertAsync(providerAllocation);
                        }
                    }
                }
            }
        }





        private static async Task GetSourceDataAsync()
        {
            var reader = new ExcelReader();
            using (var repository = new Repository("datasets"))
            {
                var aptSourceRecords =
                    reader.Read<AptSourceRecord>(@"SourceData\Export APT.XLSX").ToArray();

                var numberCountSourceRecords =
                    reader.Read<NumberCountSourceRecord>(@"SourceData\Number Counts Export.XLSX").ToArray();

                foreach (var aptSourceRecord in aptSourceRecords)
                {
                    var providerInformation = new AptProviderInformation
                    {
                        ModelName = typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(AptProviderInformation).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        URN = aptSourceRecord.URN,
                        DateOpened = aptSourceRecord.DateOpened,
                        LocalAuthority = aptSourceRecord.LocalAuthority,
                        ProviderName = aptSourceRecord.ProviderName,
                        UPIN = aptSourceRecord.UPIN
                    };
                    await repository.UpsertAsync(providerInformation);

                    var basicEntitlement = new AptBasicEntitlement
                    {
                        ModelName = typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(AptBasicEntitlement).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        URN = aptSourceRecord.URN,
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
                        ModelName = typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().ModelName,
                        DatasetName = typeof(CensusNumberCounts).GetCustomAttribute<DatasetAttribute>().DatasetName,
                        URN = numberCountSourceRecord.URN,
                        NORPrimary =numberCountSourceRecord.NumberOnRollPrimary
                    };
                    await repository.UpsertAsync(censusNumberCount);

                }
            }


        }
    }
}
