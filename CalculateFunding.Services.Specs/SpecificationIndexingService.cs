using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationIndexingService : JobProcessingService, ISpecificationIndexingService
    {
        private const string ReIndexSpecificationJob = JobConstants.DefinitionNames.ReIndexSpecificationJob;
        private const string SpecificationId = "specification-id";

        private readonly IJobManagement _jobs;
        private readonly ILogger _logger;
        private readonly ISpecificationIndexer _indexer;
        private readonly ISpecificationsRepository _specifications;
        private readonly AsyncPolicy _specificationsPolicy;

        public SpecificationIndexingService(IJobManagement jobs,
            ILogger logger,
            ISpecificationIndexer indexer,
            ISpecificationsRepository specifications,
            ISpecificationsResiliencePolicies resiliencePolicies) : base(jobs, logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(indexer, nameof(indexer));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsRepository, nameof(resiliencePolicies.SpecificationsRepository));

            _jobs = jobs;
            _indexer = indexer;
            _specifications = specifications;
            _specificationsPolicy = resiliencePolicies.SpecificationsRepository;
        }

        public async Task<IActionResult> QueueSpecificationIndexJob(string specificationId,
            Reference user,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            return new OkObjectResult(await _jobs.QueueJob(new JobCreateModel
            {
                JobDefinitionId = ReIndexSpecificationJob,
                InvokerUserId = user?.Id,
                CorrelationId = correlationId,
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    Message = "Specification change requires reindexing",
                    EntityType = nameof(Specification),
                    EntityId = specificationId
                },
                Properties = new Dictionary<string, string>
                {
                    {
                        SpecificationId, specificationId
                    }
                }
            }));
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = GetMessageProperty(message, SpecificationId);
            Specification specification = await _specificationsPolicy.ExecuteAsync(() => _specifications.GetSpecificationById(specificationId));

            if (specification == null)
            {
                throw new ArgumentOutOfRangeException(nameof(specificationId), $@"Did not locate a specification {specificationId} to index");
            }

            await _indexer.Index(specification);
        }

        private string GetMessageProperty(Message message,
            string name)
            => message.GetUserProperty<string>(name);
    }
}