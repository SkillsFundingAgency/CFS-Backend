﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Helpers;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class GraphRepository : IGraphRepository
    {
        private readonly IGraphApiClient _graphApiClient;
        private readonly Policy _resilience;
        private readonly ILogger _logger;
        private readonly ICalculationsFeatureFlag _calculationsFeatureFlag;

        public GraphRepository(IGraphApiClient graphApiClient,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger,
            ICalculationsFeatureFlag calculationsFeatureFlag)
        {
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.GraphApiClientPolicy, nameof(resiliencePolicies.GraphApiClientPolicy));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationsFeatureFlag, nameof(calculationsFeatureFlag));

            _graphApiClient = graphApiClient;
            _logger = logger;
            _resilience = resiliencePolicies.GraphApiClientPolicy;
            _calculationsFeatureFlag = calculationsFeatureFlag;
        }

        public async Task PersistToGraph(IEnumerable<Calculation> calculations, SpecificationSummary specification, string calculationId = null, bool withDelete = false)
        {
            if (!(await _calculationsFeatureFlag.IsGraphEnabled()))
            {
                return;
            }

            await _resilience.ExecuteAsync(() => _graphApiClient.UpsertSpecifications(new[] {new Common.ApiClient.Graph.Models.Specification
            {
                SpecificationId = specification.Id,
                Description = specification.Description,
                Name = specification.Name
            }}));

            // get all calculation functions
            IDictionary<string, string> functions = calculations.ToDictionary(_ => $"{_.Namespace}.{_.Current.SourceCodeName}", _ => _.Id);

            // if a calculationId is sent in then we need to filter out all other calcs so we don't redo all 
            IEnumerable<Calculation> currentCalculations = calculationId != null ? calculations.Where(_ => _.Id == calculationId) : calculations;

            // this is on an edit calculation so delete the existing calc first
            if (withDelete)
            {
                IEnumerable<Task> deleteTasks = currentCalculations.Select(async (_) =>
                {
                    await _resilience.ExecuteAsync(() => _graphApiClient.DeleteCalculation(_.Id));
                });

                await TaskHelper.WhenAllAndThrow(deleteTasks.ToArray());
            }

            await _resilience.ExecuteAsync(() => _graphApiClient.UpsertCalculations(currentCalculations.Select(_ => new Common.ApiClient.Graph.Models.Calculation
            {
                CalculationId = _.Id,
                CalculationName = _.Current.Name,
                CalculationType = _.Current.CalculationType.AsMatchingEnum<Common.ApiClient.Graph.Models.CalculationType>(),
                FundingStream = _.FundingStreamId,
                SpecificationId = _.SpecificationId,
                TemplateCalculationId = _.Id
            }).ToArray()));

            IEnumerable<Task> calcSpecTasks = currentCalculations.Select(async (_) =>
            {
                await _resilience.ExecuteAsync(() => _graphApiClient.UpsertCalculationSpecificationRelationship(_.Id, specification.Id));
            });

            IEnumerable<Task> tasks = currentCalculations.Select(async (calculation) =>
            {
                IEnumerable<string> references = SourceCodeHelpers.GetReferencedCalculations(functions.Keys, calculation.Current.SourceCode);

                if (references.Any())
                {
                    await _resilience.ExecuteAsync(async () => await _graphApiClient.UpsertCalculationCalculationsRelationships(calculation.Id,
                            references.Select(_ => functions[_]).ToArray()));
                }
            });

            calcSpecTasks = calcSpecTasks.Concat(tasks);

            await TaskHelper.WhenAllAndThrow(calcSpecTasks.ToArray());
        }
    }
}