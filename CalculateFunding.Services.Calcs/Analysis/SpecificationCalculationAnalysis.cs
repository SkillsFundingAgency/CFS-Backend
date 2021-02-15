using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Compiler.Interfaces;
using Polly;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using GraphCalculation = CalculateFunding.Models.Graph.Calculation;
using GraphFundingLine = CalculateFunding.Models.Graph.FundingLine;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class SpecificationCalculationAnalysis : ISpecificationCalculationAnalysis
    {
        private readonly AsyncPolicy _specificationsResilience;
        private readonly AsyncPolicy _calculationsResilience;
        readonly ISpecificationsApiClient _specifications;
        private readonly ICalculationsRepository _calculations;
        private readonly ICalculationAnalysis _calculationAnalysis;
        private readonly IBuildProjectsService _buildProjectsService;
        private readonly IDatasetReferenceService _datasetReferenceService;
        private readonly IMapper _mapper;

        public SpecificationCalculationAnalysis(ICalcsResiliencePolicies policies, 
            ISpecificationsApiClient specifications, 
            ICalculationsRepository calculations, 
            ICalculationAnalysis calculationAnalysis,
            IBuildProjectsService buildProjectsService,
            IDatasetReferenceService datasetReferenceService,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(policies?.CalculationsRepository, nameof(policies.CalculationsRepository));
            Guard.ArgumentNotNull(policies?.SpecificationsApiClient, nameof(policies.SpecificationsApiClient));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(calculationAnalysis, nameof(calculationAnalysis));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));
            Guard.ArgumentNotNull(datasetReferenceService, nameof(datasetReferenceService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _specificationsResilience = policies.SpecificationsApiClient;
            _calculationsResilience = policies.CalculationsRepository;
            _specifications = specifications;
            _calculations = calculations;
            _calculationAnalysis = calculationAnalysis;
            _buildProjectsService = buildProjectsService;
            _datasetReferenceService = datasetReferenceService;
            _mapper = mapper;
        }

        public async Task<SpecificationCalculationRelationships> GetSpecificationCalculationRelationships(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<SpecificationSummary> specificationResponse = await _specificationsResilience.ExecuteAsync(() =>
                _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specificationSummary = specificationResponse?.Content;
            
            if (specificationSummary == null)
            {
                throw new ArgumentOutOfRangeException(nameof(specificationId), 
                    $"No specification with id {specificationId}. Unable to get calculation relationships");
            }
            
            Calculation[] calculations = (await _calculationsResilience.ExecuteAsync(() =>
                _calculations.GetCalculationsBySpecificationId(specificationId)))?.ToArray();

            if (calculations.IsNullOrEmpty())
            {
                throw new ArgumentOutOfRangeException(nameof(specificationId), 
                    $"No calculations for specification with id {specificationId}. Unable to get calculation relationships");   
            }

            IEnumerable<CalculationRelationship> calculationRelationships = _calculationAnalysis.DetermineRelationshipsBetweenCalculations(calculations);

            BuildProject buildProject = await _buildProjectsService.GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<FundingLineCalculationRelationship> fundingLineRelationships = _calculationAnalysis.DetermineRelationshipsBetweenFundingLinesAndCalculations(calculations, buildProject.FundingLines);

            calculationRelationships = calculationRelationships.Concat(fundingLineRelationships.Select(_ => new CalculationRelationship { CalculationOneId = _.CalculationOneId, CalculationTwoId = _.CalculationTwoId }));

            IEnumerable<DatasetReference> datasetReferences = _datasetReferenceService.GetDatasetRelationShips(calculations, buildProject.DatasetRelationships);

            return new SpecificationCalculationRelationships
            {
                Specification = _mapper.Map<Specification>(specificationSummary),
                Calculations = _mapper.Map<IEnumerable<GraphCalculation>>(calculations),
                FundingLines = _mapper.Map<IEnumerable<GraphFundingLine>>(buildProject.FundingLines?.Values.SelectMany(_ => _.FundingLines)),
                CalculationRelationships = calculationRelationships,
                FundingLineRelationships = fundingLineRelationships,
                CalculationDataFieldRelationships = datasetReferences.SelectMany(_ => _.Calculations.Select(calc => new CalculationDataFieldRelationship { Calculation = calc, DataField = _.DataField })).Distinct(),
                DatasetDataFieldRelationships = datasetReferences.Select(_ => new DatasetDataFieldRelationship { Dataset = _.Dataset, DataField = _.DataField }),
                DatasetDatasetDefinitionRelationships = datasetReferences.Select(_ => new DatasetDatasetDefinitionRelationship { Dataset = _.Dataset, DatasetDefinition = _.DatasetDefinition })
            };
        }
    }
}