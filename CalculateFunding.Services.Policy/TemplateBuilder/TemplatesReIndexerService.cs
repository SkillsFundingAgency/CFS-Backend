using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplatesReIndexerService : ITemplatesReIndexerService
    {
        private readonly ILogger _logger;
        private readonly IJobManagement _jobManagement;
        private readonly ISearchRepository<TemplateIndex> _searchRepository;
        private readonly AsyncPolicy _searchRepositoryResilience;
        private readonly ITemplateRepository _templatesRepository;
        private readonly AsyncPolicy _templatesRepositoryResilience;

        private const int BatchSize = 1000;

        public TemplatesReIndexerService(ISearchRepository<TemplateIndex> searchRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IPolicyRepository policyRepository,
            ITemplateRepository templateRepository,
            IJobManagement jobManagement,
            ILogger logger)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.TemplatesSearchRepository,
                nameof(policyResiliencePolicies.TemplatesSearchRepository));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository,
                nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(templateRepository, nameof(templateRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.TemplatesRepository,
                nameof(policyResiliencePolicies.TemplatesRepository));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _searchRepository = searchRepository;
            _searchRepositoryResilience = policyResiliencePolicies.TemplatesSearchRepository;
            _templatesRepository = templateRepository;
            _templatesRepositoryResilience = policyResiliencePolicies.TemplatesRepository;
            _jobManagement = jobManagement;
            _logger = logger;
        }

        public async Task Run(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference user = message.GetUserDetails();

            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            JobViewModel currentJob;

            try
            {
                currentJob = await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch (Exception e)
            {
                string errorMessage = "Job cannot be run";
                _logger.Error(e, errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            try
            {
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, null);

                await _templatesRepositoryResilience.ExecuteAsync(() => _templatesRepository.GetTemplatesForIndexing(
                    async templates =>
                    {
                        IList<TemplateIndex> results = new List<TemplateIndex>();

                        foreach (Template template in templates)
                        {
                            results.Add(new TemplateIndex
                            {
                                Id = template.TemplateId,
                                Name = template.Name,
                                FundingStreamId = template.FundingStream.Id,
                                FundingStreamName = template.FundingStream.Name,
                                FundingPeriodId = template.FundingPeriod.Id,
                                FundingPeriodName = template.FundingPeriod.Name,
                                LastUpdatedAuthorId = template.Current.Author?.Id,
                                LastUpdatedAuthorName = template.Current.Author?.Name,
                                LastUpdatedDate = template.Current.Date,
                                Version = template.Current.Version,
                                Status = template.Current.Status.ToString(),
                                CurrentMajorVersion = template.Current.MajorVersion,
                                CurrentMinorVersion = template.Current.MinorVersion,
                                PublishedMajorVersion = template.Released?.MajorVersion ?? 0,
                                PublishedMinorVersion = template.Released?.MinorVersion ?? 0,
                                HasReleasedVersion = template.Released?.Status != null ? "Yes" : "No"
                            });
                        }

                        IEnumerable<IndexError> errors =
                            await _searchRepositoryResilience.ExecuteAsync(() => _searchRepository.Index(results));

                        if (errors != null && errors.Any())
                        {
                            string errorMessage =
                                $"Failed to index published provider documents with errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}";

                            _logger.Error(errorMessage);

                            throw new NonRetriableException(errorMessage);
                        }
                    },
                    BatchSize));

                _logger.Information($"Completing templates reindex job. JobId='{jobId}'");
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
                _logger.Information($"Completed templates reindex job. JobId='{jobId}'");
            }
            catch (Exception)
            {
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, false, null);
                throw;
            }
        }
    }
}
