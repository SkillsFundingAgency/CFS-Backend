using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.LocalDebugProxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            RegisterComponents(services);
            services.AddMvc();

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }

        void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

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
                .AddScoped<IBuildProjectsService, BuildProjectsService>();

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

            builder
                .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
                .AddScoped<ICalcsRepository, CalcsRepository>();

            builder
               .AddScoped<ICalculationEngine, CalculationEngine>();

            builder
              .AddScoped<IAllocationFactory, AllocationFactory>();

            builder
                .AddScoped<ISpecificationRepository, SpecificationRepository>();
            builder
               .AddScoped<Services.Datasets.Interfaces.IProviderResultsRepository, Services.Datasets.ProviderResultsRepository>();

            builder
              .AddScoped<Services.Calcs.Interfaces.IProviderResultsRepository, Services.Calcs.ProviderResultsRepository>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<SpecificationsMappingProfile>(); c.AddProfile<DatasetsMappingProfile>(); });
            builder
                .AddSingleton(mappingConfig.CreateMapper());

            //MapperConfiguration dataSetsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            //builder
            //    .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddSearch(config);

            builder.AddHttpEventHub(config);

            builder.AddInterServiceClient(config);

            builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

            builder.AddScoped<ILogger>(l => new LoggerConfiguration().WriteTo.Console().CreateLogger());

            builder.AddCaching(config);
        }
    }
}
