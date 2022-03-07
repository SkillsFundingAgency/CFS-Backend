using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlImportService : JobProcessingService, ISqlImportService
    {
        private const string RunSqlImportJob = JobConstants.DefinitionNames.RunSqlImportJob;
        private const string RunReleasedSqlImportJob = JobConstants.DefinitionNames.RunReleasedSqlImportJob;

        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "funding-stream-id";

        private readonly ISqlImporter _import;
        private readonly IQaSchemaService _schema;

        public SqlImportService(ISqlImporter import, 
            IQaSchemaService schema, 
            IJobManagement jobManagement,
            ILogger logger) 
            : base(jobManagement, logger)
        {
            _import = import;
            _schema = schema;
        }

        public async Task<IActionResult> QueueSqlImport(string specificationId,
            string fundingStreamId,
            Reference user,
            string correlationId,
            SqlExportSource sqlExportSource)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            Job job = await QueueJob(new JobCreateModel
            {
                JobDefinitionId = sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion ? RunSqlImportJob : RunReleasedSqlImportJob,
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                Trigger = new Trigger
                {
                    Message = "Running sql import for specification and funding stream",
                    EntityId = specificationId,
                    EntityType = "Specification"
                },
                Properties = new Dictionary<string, string>
                {
                    {SpecificationId, specificationId},
                    {FundingStreamId, fundingStreamId}
                }
            });
            
            return new OkObjectResult(new
            {
                JobId = job.Id
            });
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.GetUserProperty<string>(SpecificationId);
            string fundingStreamId = message.GetUserProperty<string>(FundingStreamId);
            
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            SqlExportSource sqlExportSource = 
                Job.JobDefinitionId == RunSqlImportJob ? 
                SqlExportSource.CurrentPublishedProviderVersion : 
                SqlExportSource.ReleasedPublishedProviderVersion;

            SchemaContext schemaContext = await _schema.ReCreateTablesForSpecificationAndFundingStream(
                specificationId, 
                fundingStreamId,
                Job.Id,
                sqlExportSource);
            
            await _import.ImportData(specificationId, fundingStreamId, schemaContext, sqlExportSource);
        }
    }
}