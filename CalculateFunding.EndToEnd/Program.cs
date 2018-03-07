using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Validators;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Serilog;
using YamlDotNet.Core.Tokens;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using StatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax;
using NSubstitute;
using AllocationFactory = CalculateFunding.Services.Calculator.AllocationFactory;
using Substitute = NSubstitute.Substitute;

namespace CalculateFunding.EndToEnd
{

    class Program
    {
		static void Main(string[] args)
		{
			var serviceCollection = new ServiceCollection();
			RegisterComponents(serviceCollection);
			var serviceProvider = serviceCollection.BuildServiceProvider();

			var specService = serviceProvider.GetService<ISpecificationsService>();
			var buildProjectRepo = serviceProvider.GetService<IBuildProjectsRepository>();
			var dataRelationshipService = serviceProvider.GetService<IDefinitionSpecificationRelationshipService>();
			var specSearchService = serviceProvider.GetService<ISpecificationsSearchService>();
			var providerSearchService = serviceProvider.GetService<IResultsSearchService>();
			var resultsRepo = serviceProvider.GetService<IResultsRepository>();
			var calcService = serviceProvider.GetService<ICalculationService>();
			var calc = serviceProvider.GetService<CalculationEngine>();
		    var provider = serviceProvider.GetService<ISourceFileGeneratorProvider>();
		    var compilerFactory = serviceProvider.GetService<ICompilerFactory>();

            ConsoleLogger logger = new ConsoleLogger("Default", (s, level) => true, true);

			var user = new Reference("matt.hammond@education.gov.uk", "Matt Hammond");

			var testSearchIndex = new SearchRepository<ProviderIndex>(new SearchRepositorySettings
			{
				SearchServiceName = "esfacfsbtest-search",
				SearchKey = "28E14ED3D00574840537C748ADCDFAA9"
			});

		    DatasetDefinition datasetDefinition = GetDatasetDefinition();

		    var datasetStream = File.OpenRead("SourceData/Export APT.XLSX");

		    var reader = new ExcelDatatseReader();

            // 1. ExcelReader.Read will read and validate according to a given dataset definition
            //  - this should be triggered on upload so that the validation errors can be viewed (just store them for now, story coming to view them)

		    var tableLoadResults = reader.Read(datasetStream, datasetDefinition).ToList();

		    var buildProject = new BuildProject
		    {
		        Id = "1234",
		        Name = "Test",
		        TargetLanguage = TargetLanguage.VisualBasic,
		        Specification = new SpecificationSummary
		        {
		            Id = "1234",
		            Name = "Test",
		            FundingStream = new Reference("tst", "test"),
		            Period = new Reference("tst", "test")
		        },
		        Calculations = new List<Calculation>()
		        {
		            new Calculation{ Id="1234", Name = "Get Me A Dataset!", Current = new CalculationVersion{ SourceCode = "Return Datasets.ThisYear.NORPrimary + Datasets.LastYear.NORPrimary"}},
		            new Calculation{ Id="12345", Name = "Aggregate This!", Current = new CalculationVersion{ SourceCode =
                        @"  Return Datasets.AllAuthorityProviders.Count"}}
		        }
            };


 
            if (buildProject != null)
		    {
		        // 2. When a data relationship is added to a spec a message needs to be sent to calcs to populate the dataset relatioships on the buildproject, with
		        // the buildproject being recompiled when this is added (This build the typesafe classes for the dataset and adds the Dataset.XXX properties

                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>
		        {
		            new DatasetRelationshipSummary{ Id = "1", Name = "This Year", DatasetDefinition = datasetDefinition},
		            new DatasetRelationshipSummary{ Id = "2", Name = "Last Year", DatasetDefinition = datasetDefinition},
		            new DatasetRelationshipSummary{ Id = "3", Name = "All Authority Providers", DatasetDefinition = datasetDefinition, DataGranularity = DataGranularity.MultipleRowsPerProvider},
                };

		        var generator = provider.CreateSourceFileGenerator(buildProject.TargetLanguage);
		        var sourceFiles = generator.GenerateCode(buildProject);

		        ICompiler compiler = compilerFactory.GetCompiler(sourceFiles);

		        buildProject.Build = compiler.GenerateCode(sourceFiles?.ToList());

                // FYI - it is really useful to look at the actual code generated to understand the engine - just load the project in VS
		        foreach (var sourceFile in sourceFiles)
		        {
		            Directory.CreateDirectory($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Source\Repos\CalculateFunding-Backend\Spikes\VisualBasic\{Path.GetDirectoryName(sourceFile.FileName)}");
		            File.WriteAllText($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Source\Repos\CalculateFunding-Backend\Spikes\VisualBasic\{sourceFile.FileName}", sourceFile.SourceCode);
		        }

		        var assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));
		        var allocationFactory = new AllocationFactory(assembly);
		        var allocationModel = allocationFactory.CreateAllocationModel();

		        var providerSummary = new ProviderSummary { UPIN = "124121"};

		        var providerDatasets = new List<ProviderSourceDataset>();

                // 3. When a specific dataset version is mapped to a spec relationship a message should be sent to results to store a providersourcedataset for each
                // valid row in the table load results
                // Once complete this should also trigger the calc engine to re-run

		        foreach (var loadResult in tableLoadResults)
		        {
		            foreach (var dataRelationship in buildProject.DatasetRelationships)
		            {
		                providerDatasets.Add(new ProviderSourceDataset
		                {
                            DataGranularity = dataRelationship.DataGranularity,
                            DefinesScope = dataRelationship.DefinesScope, // states whether this defines the provider scope
                            DataDefinition = new VersionReference(dataRelationship.DatasetDefinition.Id, dataRelationship.DatasetDefinition.Name, 5),
                            DataRelationship = new VersionReference("4321", dataRelationship.Name, 5),
		                    Current = new SourceDataset
		                    {
		                        Dataset = new VersionReference("apt", "APT 1819", 4),
		                        Rows = loadResult.Rows.Where(x => x.Identifier == providerSummary.UPIN && x.IdentifierFieldType == IdentifierFieldType.UPIN).Select(x => x.Fields).ToList()
		                    }
		                });
                    }

		        }

                // 4. This is a single hard coded provider - in reality we need to  implement a repo to load for each provider
                // TODO - work out the best way of storing/loading the scoped provider list
                var results = calc.CalculateProviderResults(allocationModel, buildProject, providerSummary, providerDatasets);
		    }
		}

        private static DatasetDefinition GetDatasetDefinition()
        {
            return new DatasetDefinition
            {
                Id = "test-apt",
                Name = "APT",
                Description = "Test APT Dataset Schema",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition
                    {
                        Id = "table-1",
                        Name = "*",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition{ Name= "UPIN", Type = FieldType.String, IdentifierFieldType = IdentifierFieldType.UPIN},
                            new FieldDefinition{ Name= "Date Opened", Type = FieldType.DateTime},
                            new FieldDefinition{ Name= "Phase", Type = FieldType.String},
                            new FieldDefinition{ Name= "Acedemy Type", Type = FieldType.String},
                            new FieldDefinition{ Name= "NOR Primary", Type = FieldType.Integer},
                            new FieldDefinition{ Name= "Average Year Group Size", Type = FieldType.Decimal},
                            
                        }
                    }
                }

            };
        }

        static void RegisterComponents(IServiceCollection builder)
		{
			IConfigurationRoot config = ConfigHelper.AddConfig();

			builder
				.AddScoped<ICalculationsRepository, CalculationsRepository>();

			builder.AddScoped<ICalculationsRepository, CalculationsRepository>((ctx) =>
			{
				CosmosDbSettings calssDbSettings = new CosmosDbSettings();

				config.Bind("CosmosDbSettings", calssDbSettings);

				calssDbSettings.CollectionName = "calcs";

				CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

				return new CalculationsRepository(calcsCosmosRepostory);
			});

			builder
			   .AddScoped<IValidator<CreateNewDatasetModel>, CreateNewDatasetModelValidator>();

			builder
				.AddScoped<IBlobClient, BlobClient>((ctx) =>
				{
					AzureStorageSettings storageSettings = new AzureStorageSettings();

					config.Bind("AzureStorageSettings", storageSettings);

					storageSettings.ContainerName = "datasets";

					return new BlobClient(storageSettings);
				});

			builder
			  .AddScoped<IDatasetService, DatasetService>();

			builder.AddScoped<IDatasetRepository, DataSetsRepository>((ctx) =>
			{
				CosmosDbSettings datasetsDbSettings = new CosmosDbSettings();

				config.Bind("CosmosDbSettings", datasetsDbSettings);

				datasetsDbSettings.CollectionName = "datasets";

				CosmosRepository datasetsCosmosRepostory = new CosmosRepository(datasetsDbSettings);

				return new DataSetsRepository(datasetsCosmosRepostory);
			});

			builder
			   .AddScoped<ICalculationService, CalculationService>();

			builder
			  .AddScoped<ICalculationsSearchService, CalculationSearchService>();

			builder
				.AddScoped<IDatasetSearchService, DatasetSearchService>();

			builder
				.AddScoped<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>();

			builder
				.AddScoped<Services.Datasets.Interfaces.ISpecificationsRepository, Services.Datasets.SpecificationsRepository>();

			builder
				.AddScoped<IValidator<Models.Calcs.Calculation>, CalculationModelValidator>();

			builder
				.AddScoped<IDefinitionsService, DefinitionsService>();

			builder
				.AddScoped<ISpecificationsSearchService, SpecificationsSearchService>();


			builder
				.AddScoped<IResultsSearchService, ResultsSearchService>();

			builder
				.AddScoped<IResultsService, ResultsService>();


			builder.AddScoped<IResultsRepository, ResultsRepository>((ctx) =>
			{
				CosmosDbSettings specsDbSettings = new CosmosDbSettings();

				config.Bind("CosmosDbSettings", specsDbSettings);

				specsDbSettings.CollectionName = "results";

				CosmosRepository specsCosmosRepostory = new CosmosRepository(specsDbSettings);

				return new ResultsRepository(specsCosmosRepostory);
			});

			builder
				.AddScoped<CalculationEngine, CalculationEngine>();


			builder.AddScoped<Services.Specs.Interfaces.ISpecificationsRepository, Services.Specs.SpecificationsRepository>((ctx) =>
			{
				CosmosDbSettings specsDbSettings = new CosmosDbSettings();

				config.Bind("CosmosDbSettings", specsDbSettings);

				specsDbSettings.CollectionName = "specs";

				CosmosRepository specsCosmosRepostory = new CosmosRepository(specsDbSettings);

				return new Services.Specs.SpecificationsRepository(specsCosmosRepostory);
			});

			builder.AddScoped<IBuildProjectsRepository, BuildProjectsRepository>((ctx) =>
			{
				CosmosDbSettings calssDbSettings = new CosmosDbSettings();

				config.Bind("CosmosDbSettings", calssDbSettings);

				calssDbSettings.CollectionName = "calcs";

				CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

				return new BuildProjectsRepository(calcsCosmosRepostory);
			});

			builder
				.AddScoped<IPreviewService, PreviewService>();

			builder
			   .AddScoped<ICompilerFactory, CompilerFactory>();

			builder
				.AddScoped<CSharpCompiler>()
				.AddScoped<VisualBasicCompiler>()
				.AddScoped<VisualBasicSourceFileGenerator>();

			builder
			  .AddScoped<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();

			builder
			   .AddScoped<IValidator<PreviewRequest>, PreviewRequestModelValidator>();

			builder
				.AddScoped<ISpecificationsService, SpecificationsService>();

			builder
				.AddScoped<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();

			builder
				.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();

			builder
				.AddScoped<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();

			builder
			   .AddScoped<IValidator<CreateNewDatasetModel>, CreateNewDatasetModelValidator>();

			builder
				.AddScoped<IValidator<DatasetMetadataModel>, DatasetMetadataModelValidator>();

			builder
				.AddScoped<IValidator<GetDatasetBlobModel>, GetDatasetBlobModelValidator>();

			builder
				.AddScoped<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();

			builder
				.AddScoped<IValidator<CreateDefinitionSpecificationRelationshipModel>, CreateDefinitionSpecificationRelationshipModelValidator>();

			MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<SpecificationsMappingProfile>(); c.AddProfile<DatasetsMappingProfile>(); });
			builder
				.AddSingleton(mappingConfig.CreateMapper());

			//MapperConfiguration dataSetsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
			//builder
			//    .AddSingleton(dataSetsConfig.CreateMapper());

			builder.AddSearch(config);

			builder.AddServiceBus(config);

			builder.AddInterServiceClient(config);

			builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

			builder.AddScoped<Serilog.ILogger>(l => new LoggerConfiguration().WriteTo.Console().CreateLogger());
		}


        private static string ConvertTheStoreScript(Product product)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(product.Script);

            var function = tree.GetRoot().DescendantNodes().FirstOrDefault(x => x.Kind() == SyntaxKind.FunctionBlock);
            var statements = function.ChildNodes().OfType<StatementSyntax>().ToList();

            var builder = new StringBuilder();
            foreach (var statement in statements)
            {
                var kind = statement.Kind();
                switch (kind)
                {
                    case SyntaxKind.FunctionStatement:
                    case SyntaxKind.EndFunctionStatement:
                        break;
                    default:
                        var line = statement.ToFullString();




                        line = Regex.Replace(line, @"\[Datasets.(\S+)\]", "Datasets.$1", RegexOptions.IgnoreCase);
                        line = line.Replace("Products.1718_Global_Variables.", "");
                        line = line.Replace("products.1718_Global_Variables.", "");

                       

                        line = line.Replace("Products.1718_SBS.", "");

                        line = line.Replace("products.1718_SBS.", "");
                        line = line.Replace("Products.1718_NOR.", "");
                        line = line.Replace("products.1718_NOR.", "");

                        line = line.Replace("As Double", "As Decimal");
                        line = line.Replace("Dim result = 0", "Dim result = Decimal.Zero");

                        line = line.Replace(
                            "datasets.Administration.Providers.Academy_Information.Academy_Parameters.Funding_Basis(2017181)",
                            "Datasets.AcademyInformation.FundingBasis");

                        line = line.Replace(
                            "Datasets.Administration.Providers.Academy_Information.Academy_Parameters.Funding_Basis(2017181)",
                            "Datasets.AcademyInformation.FundingBasis");

 

                        var matches = Regex.Matches(line, @"Datasets.(\S+).(\S+).(\S+).(\S+).(\S+)", RegexOptions.IgnoreCase);
                        if (matches.Count > 0)
                        {
                            var match = matches[0].Value;
                            var split = match.Split('.');
                            if (!split.Last().Contains("Funding_Basis"))
                            {
                                var dataset = split[split.Length - 2].Replace("_", "");
                                var field = split[split.Length - 1].Replace("_", "").Replace(" ", "").Replace(@"/", "").Replace("-", "");
                                line = line.Replace(match, $"Datasets.{dataset}.{field}");
                            }

                        }
                        if (line.ToLowerInvariant().Contains("datasets.academy_allocations"))
                        {


                            line = Regex.Replace(line, @"Datasets.(\S+).(\S+).(\S+).(\S+).(\S+)", "$4.$5");
                        }

                        line = line.Replace("Datasets.ProviderInformation.", "Provider.");

                        var dimAsMatch = Regex.Match(line, @"Dim (\w*) As \w* = (\w*)");

                        if (dimAsMatch.Success)
                        {
                            if (dimAsMatch.Groups[1].Value == dimAsMatch.Groups[2].Value)
                            {
                                break;
                            }

                        }


                        line = line.Replace("APTNewISBdataset.17–18", "APTNewISBdataset._1718");


                        dimAsMatch = Regex.Match(line, @"Dim (\w*) = (\w*)");

                        if (dimAsMatch.Success)
                        {
                            if (dimAsMatch.Groups[1].Value == dimAsMatch.Groups[2].Value)
                            {
                                break;
                            }

                        }

                        line = line.Replace(product.Name, $"{product.Name}_Local");


                        builder.AppendLine(line);
                        break;
                }
            }

            return builder.ToString();
        }

        private static async Task<Specification> ImportSpecification(Reference user, ConsoleLogger logger)
        {
            var specJson = File.ReadAllText(Path.Combine("SourceData", "spec.json"));

            var spec = JsonConvert.DeserializeObject<Specification>(specJson);

            //await Specifications.RunCommands(GetHttpRequest(new SpecificationCommand
            //{
            //    Content = spec,
            //    Method = CommandMethod.Post,
            //    Id = Reference.NewId(),
            //    User = user
            //}), logger);

            return spec;
        }

        public class Product
        {
            public string Name { get; set; }
            public string Script { get; set; }
            public string FolderName { get; set; }
            public string ScenarioName { get; set; }
            public string ProductType { get; set; }
            public int DecimalPlaces { get; set; }
        }

        private static List<Product> ImportProducts()
        {
            var json = File.ReadAllText(Path.Combine("SourceData", "products.json"));

            var products = JsonConvert.DeserializeObject<List<Product>>(json);


            return products;
        }


        //private static async Task<SpecificationScope> ImportProviders(Specification specification, Reference user, ConsoleLogger logger)
        //{
        //    using (var blob = File.Open(Path.Combine("SourceData", "edubasealldata20171122.csv"), FileMode.Open))
        //    {

        //        var stopWatch = new Stopwatch();
        //        stopWatch.Start();

        //        using (var reader = new StreamReader(blob))
        //        {
        //           // var providers = new EdubaseImporterService().ImportEdubaseCsv(reader).ToList();


        //            //stopWatch.Stop();
        //            //Console.WriteLine($"Read {providers.Count} providers in {stopWatch.ElapsedMilliseconds}ms");
        //            //stopWatch.Restart();

        //            //var scope = new SpecificationScope
        //            //{
        //            //    Id = Reference.NewId(),
        //            //    Name = specification.Name,
        //            //    Specification = specification.GetReference(),
        //            //    Providers = providers.Select(x => new ProviderSummary
        //            //    {
        //            //        Id = x.Id,
        //            //        URN = x.URN,
        //            //        Name = x.Name,
        //            //        Authority = x.Authority,
        //            //        Phase = x.PhaseOfEducation,
        //            //        Tags = new List<string>()
        //            //    }).ToList()
        //            //};

        //            //await SpecificationScopes.RunCommands(GetHttpRequest(new SpecificationScopeCommand
        //            //{
        //            //    Content = scope,
        //            //    Method = CommandMethod.Post,
        //            //    Id = Reference.NewId(),
        //            //    User = user
        //            //}), logger);
        //            File.WriteAllText("scope.json", JsonConvert.SerializeObject(scope));
        //            return scope;



        //            //foreach (var provider in providers.Take(5))
        //            //{
        //            //    await Providers.RunCommands(GetHttpRequest(new ProviderCommand
        //            //    {
        //            //        Content = provider,
        //            //        Method = CommandMethod.Post,
        //            //        Id =  Reference.NewId(),
        //            //        User = user
        //            //    }), logger);
        //            //}
        //        }

        //    }
        //}

        private static HttpRequest GetHttpRequest<T>(T payload, string paramName = null, string paramValue = null) 
        {
            var httpRequest = new FakeHttpRequest{Method = "POST"};
            var json = JsonConvert.SerializeObject(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            var ms = new MemoryStream(buffer);
            ms.Seek(0, SeekOrigin.Begin);
            httpRequest.Body = ms;
	        if (paramName != null)
	        {
		        httpRequest.QueryString = new QueryString($"?{paramName}={paramValue}");
				httpRequest.Query = new FakeQueryCollection{  new KeyValuePair<string, StringValues>(paramName, new StringValues(paramValue))};

			}

            return httpRequest;
        }

        public static IConfigurationRoot Configuration { get; set; }


    }

	public class FakeQueryCollection : List<KeyValuePair<string, StringValues>>, IQueryCollection
	{
		public bool ContainsKey(string key)
		{
			return ToArray().Any(x => x.Key == key);
		}

		public bool TryGetValue(string key, out StringValues value)
		{
			if (!ContainsKey(key)) return false;
			var pair = ToArray().FirstOrDefault(x => x.Key == key);
			value = pair.Value;
			return true;
		}

		public ICollection<string> Keys => ToArray().Select(x => x.Key).ToList();

		public StringValues this[string key] => ToArray().FirstOrDefault(x => x.Key == key).Value;
	}

    public class FakeHttpRequest : HttpRequest
    {
	    public FakeHttpRequest()
	    {
		    ClaimsPrincipal principle = new ClaimsPrincipal(new[]
		    {
			    new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, "matt.hammond@education.gov.uk"), new Claim(ClaimTypes.Name, "Matt Hammond") })
		    });

		    HttpContext = Substitute.For<HttpContext>();
			HttpContext
				.User
			    .Returns(principle);
	    }

	    public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override HttpContext HttpContext { get; }
        public override string Method { get; set; }
        public override string Scheme { get; set; }
        public override bool IsHttps { get; set; }
        public override HostString Host { get; set; }
        public override PathString PathBase { get; set; }
        public override PathString Path { get; set; }
        public override QueryString QueryString { get; set; }
        public override IQueryCollection Query { get; set; }
        public override string Protocol { get; set; }
        public override IHeaderDictionary Headers { get; }
        public override IRequestCookieCollection Cookies { get; set; }
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }
        public override Stream Body { get; set; }
        public override bool HasFormContentType { get; }
        public override IFormCollection Form { get; set; }
    }
}
