using CalculateFunding.ApiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.DataImporter;
using Newtonsoft.Json;
using OfficeOpenXml.Utils;

namespace CalculateFunding.ContentSync
{
    public class BaseSpecificationRecord
    {
        [SourceColumn("Id")]
        public string Id { get; set; }
        [SourceColumn("Name")]
        public string Name { get; set; }
        [SourceColumn("Description")]
        public string Description { get; set; }
    }

    public class DatasetRecord
    {
        [SourceColumn("DatasetName")]
        public string DatasetName { get; set; }
        [SourceColumn("ColumnName")]
        public string ColumnName { get; set; }
        [SourceColumn("ColumnType")]
        public string ColumnType { get; set; }
    }

    public class SpecificationRecord : BaseSpecificationRecord
    {
        [SourceColumn("Academic Year")]
        public string AcademicYear { get; set; }
        [SourceColumn("Funding Stream")]
        public string FundingStream { get; set; }
    }

    public class PolicyRecord : BaseSpecificationRecord
    {
        [SourceColumn("Parent")]
        public string Parent { get; set; }
    }

    public class CalculationRecord : BaseSpecificationRecord
    {
        [SourceColumn("Policy")]
        public string Policy { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args);

            var config = builder.Build();

            CalculateFundingApiOptions opt = new CalculateFundingApiOptions();
            config.Bind(opt);
            var services = new ServiceCollection();
            services.AddSingleton(new CalculateFundingApiClient(opt));


            var provider = services.BuildServiceProvider();

            var apiClient = provider.GetService<CalculateFundingApiClient>();

            var files = Directory.GetFiles("Content");
            foreach (var file in files.Where(x => x.ToLowerInvariant().EndsWith("2.xlsx")))
            {
                using (var blob = new FileStream(file, FileMode.Open))
                {
                    var reader = new ExcelReader();
                    var spec = reader.Read<SpecificationRecord>(blob, "Spec").First();
                    var policies = reader.Read<PolicyRecord>(blob, "Policies").ToList();
                    var calcs = reader.Read<CalculationRecord>(blob, "Calculations").ToList();
                    var datasets = reader.Read<DatasetRecord>(blob, "Datasets").ToList();


                    var specification = new Specification
                    {
                        Id = spec.Id,
                        Name = spec.Name,
                        Description = spec.Description,
                        AcademicYear = new Reference(spec.AcademicYear, spec.AcademicYear),
                        FundingStream = new Reference(spec.FundingStream, spec.FundingStream),
                        Policies = GetPolicies(policies, calcs).ToList(),
                        DatasetDefinitions = GetDatasets(datasets).ToList()
                    };

                    File.WriteAllText("spec.json", JsonConvert.SerializeObject(specification, Formatting.Indented));

                    var result = apiClient.PostSpecificationCommand(new SpecificationCommand
                    {
                        Content = specification,
                        Method = CommandMethod.Post,
                        Id = Reference.NewId(),
                        User = new Reference("matt.hammond@education.gov.uk", "Matt Hammond")
                    }).Result;

                    Console.WriteLine(result);
                }
            }





        }

        private static IEnumerable<DatasetDefinition> GetDatasets(List<DatasetRecord> datasets)
        {
            var byDatasets = datasets.GroupBy(x => x.DatasetName);
            foreach (var byDataset in byDatasets)
            {
                yield return new DatasetDefinition
                {
                    Name = byDataset.Key,
                    Description = byDataset.Key,
                    FieldDefinitions = new List<DatasetFieldDefinition>(byDataset.Select(x => new DatasetFieldDefinition
                    {
                        Name = x.ColumnName,
                        Type = Enum.Parse<FieldType>(x.ColumnType)

                    }))

                };
            }
        }

        private static IEnumerable<PolicySpecification> GetPolicies(List<PolicyRecord> policies, List<CalculationRecord> calculations, string parentName = null)
        {
            foreach (var policyRecord in policies.Where(x => x.Parent == parentName))
            {
                yield return new PolicySpecification
                {
                    Id = policyRecord.Id,
                    Name = policyRecord.Name,
                    Description = policyRecord.Description,
                    SubPolicies = GetPolicies(policies, calculations, policyRecord.Name).ToList(),
                    Calculations = calculations.Where(x => x.Policy == policyRecord.Name).Select(x =>
                        new CalculationSpecification
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Description = x.Description
                        }).ToList()
                };
            }
        }
    }
}
