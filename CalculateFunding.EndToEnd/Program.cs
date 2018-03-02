using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models;
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
using CalculateFunding.Services.CodeGeneration.CSharp;
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

			ConsoleLogger logger = new ConsoleLogger("Default", (s, level) => true, true);

			var user = new Reference("matt.hammond@education.gov.uk", "Matt Hammond");

			var testSearchIndex = new SearchRepository<ProviderIndex>(new SearchRepositorySettings
			{
				SearchServiceName = "esfacfsbtest-search",
				SearchKey = "28E14ED3D00574840537C748ADCDFAA9"
			});

			Task.Run(async () =>
			{
				var providerSummaries = new List<ProviderSummary>();
				ProviderSearchResults providers;
                var allProviders = new List<ProviderIndex>();
                var page = 1;
				do
				{
					var providersSearchResult = await providerSearchService.SearchProviders(GetHttpRequest(new SearchModel
					{
						PageNumber = page++,
						Top = 1000,
						SearchTerm = "*",
						IncludeFacets = true
					}));
					providers = (providersSearchResult as OkObjectResult)?.Value as ProviderSearchResults;

                    allProviders.AddRange(providers.Results.Select(x => new ProviderIndex
                    {
                        Name = x.Name,
                        URN = x.URN,
                        Authority = x.Authority,
                        UKPRN = x.UKPRN,
                        UPIN = x.UPIN,
                        ProviderSubType = x.ProviderSubType,
                        EstablishmentNumber = x.EstablishmentNumber,
                        ProviderType = x.ProviderType,
                        CloseDate = x.CloseDate,
                        OpenDate = x.OpenDate,
                        Rid = x.Rid
                    }));


                    providerSummaries.AddRange(providers.Results.Select(x => new ProviderSummary
					{
						Name = x.Name,
						Id = x.UKPRN,
						UKPRN = x.UKPRN,
						URN = x.URN,
						Authority = x.Authority,
						UPIN = x.UPIN,
						ProviderSubType = x.ProviderSubType,
						EstablishmentNumber = x.EstablishmentNumber,
						ProviderType = x.ProviderType
					}));
                } while (providerSummaries.Count < providers.TotalCount);


                //await testSearchIndex.Index(allProviders.ToArray());

                var specSearchResult = await specSearchService.SearchSpecifications(GetHttpRequest(new SearchModel
				{
					PageNumber = 1,
					Top = 1000,
					SearchTerm = "*",
					IncludeFacets = true
				}));
				SpecificationSearchResults specs = (specSearchResult as OkObjectResult)?.Value as SpecificationSearchResults;
				foreach (var spec in specs.Results)
				{
					var fullSpec = await specService.GetSpecificationById(GetHttpRequest("", "specificationId", spec.SpecificationId));
					var buildProject = await buildProjectRepo.GetBuildProjectBySpecificationId(spec.SpecificationId);

                    if(buildProject != null)
                    {
                        if (buildProject.Build == null)
                        {
                            await calcService.SaveCalculationVersion(GetHttpRequest(new SaveSourceCodeVersion
                            {
                                SourceCode = $"Return 42"
                            }, "calculationId", buildProject.Calculations.First().Id));

                            buildProject = await buildProjectRepo.GetBuildProjectBySpecificationId(spec.SpecificationId);
                        }


                        if (buildProject != null && ((fullSpec as OkObjectResult)?.Value as Specification) != null)
                        {
                            buildProject.Specification = new SpecificationSummary
                            {
                                Id = ((fullSpec as OkObjectResult)?.Value as Specification).Id,
                                Name = ((fullSpec as OkObjectResult)?.Value as Specification).Name,
                                FundingStream = ((fullSpec as OkObjectResult)?.Value as Specification).FundingStream,
                                Period = ((fullSpec as OkObjectResult)?.Value as Specification).AcademicYear
                            };
                            await buildProjectRepo.UpdateBuildProject(buildProject);
                            if (buildProject != null)
                            {
                                buildProject = await buildProjectRepo.GetBuildProjectBySpecificationId(spec.SpecificationId);
                                if (buildProject.Build?.AssemblyBase64 != null)
                                {
                                    var results = calc.GenerateAllocations(buildProject, providerSummaries).ToList();

                                    await resultsRepo.UpdateProviderResults(results);
                                }

                            }
                        }
                    }
                }


                    //var dataRelationships = await dataRelationshipService.GetCurrentRelationshipsBySpecificationId(GetHttpRequest("", "specificationId", spec.SpecificationId));

                    //if (dataRelationships != null)
                    //{
                    //	var relationships = ((dataRelationships as OkObjectResult)?.Value as DatasetSpecificationRelationshipViewModel[]);
                    //	if (relationships != null)
                    //	{

                    //	}
                    //}

                    



				








				//OnTimerFired.Run(null, logger);







				//foreach (var file in Directory.GetFiles("SourceData").Where(x => x.ToLowerInvariant().EndsWith(".xlsx")))
				//{
				//    using (var stream = new FileStream(file, FileMode.Open))
				//    {
				//        await importer.GetSourceDataAsync(Path.GetFileName(file), stream, budgetDefinition.Id);
				//    }
				//}


				//if (compilerOutput.Success)
				//{
				//    var calc = ServiceFactory.GetService<CalculationEngine>();
				//    await calc.GenerateAllocations(compilerOutput);
				//}
				//else
				//{
				//    foreach (var compilerMessage in compilerOutput.CompilerMessages)
				//    {
				//        Console.WriteLine(compilerMessage.Message);
				//    }
				//    Console.ReadKey();
				//}

				// await StoreAggregates(budgetDefinition, new AllocationFactory(compilerOutput.Assembly));



			}

		// Do any async anything you need here without worry
		).GetAwaiter().GetResult();
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
				.AddScoped<CSharpSourceFileGenerator>()
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
