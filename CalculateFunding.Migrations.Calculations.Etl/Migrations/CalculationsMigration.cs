using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Migrations.Calculations.Etl.Migrations
{
    public class CalculationsMigration
    {
        private readonly MigrationClients _sourceClients;
        private readonly MigrationClients _destinationClients;
        private readonly ICollection<string> _errors = new List<string>();
        private readonly bool _preventWrites;

        public CalculationsMigration(IServiceProvider sourceServices,
            IServiceProvider destinationServices,
            bool preventWrites)
        {
            Guard.ArgumentNotNull(sourceServices, nameof(sourceServices));
            Guard.ArgumentNotNull(destinationServices, nameof(destinationServices));

            _sourceClients = new MigrationClients(sourceServices);
            _destinationClients = new MigrationClients(destinationServices);
            _preventWrites = preventWrites;
        }

        public async Task Run(string sourceSpecificationId, string destinationSpecificationId)
        {
            WriteLine("Getting specifications from source and destination environments");

            (SpecificationSummary source, SpecificationSummary destination) = await GetSpecifications(sourceSpecificationId, destinationSpecificationId);

            if (MigrationHasErrors)
            {
                WriteLine("Unable to migrate calculations.");
                WriteErrors();
                PressAnyKeyToExit();

                return;
            }

            WriteLine("Checking source and destination specifications have compatible template versions");

            if (HaveDifferentTemplateVersions(source, destination))
            {
                WriteLine("Unable to migrate calculations.");
                WriteLine("The source and destination specifications have different template versions.");
                PressAnyKeyToExit();

                return;
            }

            WriteLine("Checking source and destination specifications have compatible data sources");

            if (await HaveIncompatibleDataSources(sourceSpecificationId, destinationSpecificationId))
            {
                WriteLine("Unable to migrate calculations.");
                WriteLine("The destination does not have all data sources available in the source specification.");
                PressAnyKeyToExit();

                return;
            }

            WriteLine("Migrating calculations from source specification to destination");

            if (await TryMigrateCalculations(source, destination) == false)
            {
                WriteLine("Unable to migrate calculations.");
                WriteErrors();
                PressAnyKeyToExit();

                return;
            }

            WriteLine($"Completed calculation migrations between source specification {sourceSpecificationId} and destination specification {destinationSpecificationId}");
            PressAnyKeyToExit();
        }

        private async Task<(SpecificationSummary source, SpecificationSummary destination)> GetSpecifications(string sourceSpecificationId,
            string destinationSpecificationId)
        {
            /*
                Ensure source specification exists
                Ensure destination specification exists
             */

            Task<ApiResponse<SpecificationSummary>> sourceSpecificationTask =
                GetSpecificationTask(sourceSpecificationId, _sourceClients);

            Task<ApiResponse<SpecificationSummary>> destinationSpecificationTask =
                GetSpecificationTask(destinationSpecificationId, _destinationClients);

            await TaskHelper.WhenAllAndThrow(sourceSpecificationTask, destinationSpecificationTask);

            ApiResponse<SpecificationSummary> sourceResponse = sourceSpecificationTask.Result;
            ApiResponse<SpecificationSummary> destinationResponse = destinationSpecificationTask.Result;

            CheckApiResponse(sourceResponse, $"Did not locate specification with id {sourceSpecificationId}");
            CheckApiResponse(destinationResponse, $"Did not locate specification with id {destinationSpecificationId}");

            return (sourceResponse.Content, destinationResponse.Content);
        }

        private async Task<bool> TryMigrateCalculations(SpecificationSummary sourceSpecification,
            SpecificationSummary destinationSpecification)
        {
            /*
             *  Create any missing additional calculations on destination specification
                Get the template calculation mapping for source and destination specifications for each funding streams
                Set the source code in the destination calculation for each of the calculations (based on TemplateCalculationId) where the source code is different
             */

            string sourceSpecificationId = sourceSpecification.Id;
            string destinationSpecificationId = destinationSpecification.Id;

            Task<IEnumerable<TemplateMappingItem>> sourceTemplateMappingsQueryTask = GetTemplateMappingItemsInSpecification(sourceSpecification, _sourceClients);
            Task<IEnumerable<TemplateMappingItem>> destinationTemplateMappingsQueryTask = GetTemplateMappingItemsInSpecification(destinationSpecification, _destinationClients);
            Task<ApiResponse<IEnumerable<CalculationCurrentVersion>>> sourceCalculationsQueryTask = _sourceClients.MakeCalculationsCall(
                _ => _.GetCurrentCalculationsBySpecificationId(sourceSpecificationId));
            Task<ApiResponse<IEnumerable<CalculationCurrentVersion>>> destinationCalculationsQueryTask = _sourceClients.MakeCalculationsCall(
                _ => _.GetCurrentCalculationsBySpecificationId(destinationSpecificationId));

            WriteLine("Fetching template mappings and current calculation versions from source and destination specifications");

            await TaskHelper.WhenAllAndThrow(sourceCalculationsQueryTask, destinationCalculationsQueryTask, sourceTemplateMappingsQueryTask, destinationTemplateMappingsQueryTask);

            ApiResponse<IEnumerable<CalculationCurrentVersion>> sourceCalculationsResponse = sourceCalculationsQueryTask.Result;
            ApiResponse<IEnumerable<CalculationCurrentVersion>> destinationCalculationsResponse = destinationCalculationsQueryTask.Result;

            CheckApiResponse(sourceCalculationsResponse, $"Unable to query current calculations for source specification {sourceSpecificationId}");
            CheckApiResponse(destinationCalculationsResponse, $"Unable to query current calculations for destination specification {destinationSpecificationId}");

            string additional = CalculationSpecificationType.Additional.ToString();


            CalculationCurrentVersion[] sourceAdditionalCalculations = sourceCalculationsResponse.Content.Where(
                _ => _.CalculationType == additional).ToArray();
            CalculationCurrentVersion[] destinationAdditionalCalculations = destinationCalculationsResponse.Content.Where(
                _ => _.CalculationType == additional).ToArray();

            string template = CalculationSpecificationType.Template.ToString();

            Dictionary<string, CalculationCurrentVersion> sourceTemplateCalculations = sourceCalculationsResponse.Content.Where(
                _ => _.CalculationType == template).ToDictionary(_ => _.Id, _ => _);
            Dictionary<string, CalculationCurrentVersion> destinationTemplateCalculations = destinationCalculationsResponse.Content.Where(
                _ => _.CalculationType == template).ToDictionary(_ => _.Id, _ => _);

            TemplateMappingItem[] sourceTemplateMappings = sourceTemplateMappingsQueryTask.Result.Where(
                    _ => _.EntityType == TemplateMappingEntityType.Calculation)
                .ToArray();
            Dictionary<uint, string> destinationTemplateIdToCalculationIdMappings = destinationTemplateMappingsQueryTask
                .Result.Where(_ => _.EntityType == TemplateMappingEntityType.Calculation)
                .ToDictionary(_ => _.TemplateId, _ => _.CalculationId);

            CheckForTemplateErrors(sourceTemplateMappings, destinationTemplateIdToCalculationIdMappings);

            if (MigrationHasErrors)
            {
                return false;
            }

            WriteLine("Creating missing additional calculations from source specification in destination specification");

            await CreateMissingAdditionalCalculations(sourceAdditionalCalculations, destinationAdditionalCalculations, destinationSpecificationId);

            if (MigrationHasErrors)
            {
                return false;
            }

            WriteLine("Updating source code for any template calculations in destination specification where this differs in source (using template id to match calculations)");

            await EnsureDestinationTemplateCalculationSourceCodeMatchesSource(sourceTemplateMappings,
                destinationTemplateIdToCalculationIdMappings,
                sourceTemplateCalculations,
                destinationTemplateCalculations,
                destinationSpecificationId);

            return !MigrationHasErrors;
        }

        private void CheckForTemplateErrors(TemplateMappingItem[] sourceTemplateMappings, Dictionary<uint, string> destinationTemplateIdToCalculationIdMappings)
        {
            if (sourceTemplateMappings.Any(_ => _.CalculationId.IsNullOrWhitespace()))
            {
                AddError("There are source template mapping items missing calculation ids.");
            }

            if (destinationTemplateIdToCalculationIdMappings.Any(_ => _.Value.IsNullOrWhitespace()))
            {
                AddError("There are destination template mapping items missing calculation ids.");
            }
        }

        private async Task EnsureDestinationTemplateCalculationSourceCodeMatchesSource(TemplateMappingItem[] sourceTemplateMappings,
            Dictionary<uint, string> destinationTemplateIdToCalculationIdMappings,
            Dictionary<string, CalculationCurrentVersion> sourceTemplateCalculations,
            Dictionary<string, CalculationCurrentVersion> destinationTemplateCalculations,
            string destinationSpecificationId)
        {
            foreach (TemplateMappingItem sourceTemplateMapping in sourceTemplateMappings)
            {
                if (!destinationTemplateIdToCalculationIdMappings.TryGetValue(sourceTemplateMapping.TemplateId, out string destinationCalculationId))
                {
                    AddError($"Unable to locate a destination template mapping for the source template mapping id {sourceTemplateMapping.TemplateId}");

                    continue;
                }

                if (!sourceTemplateCalculations.TryGetValue(sourceTemplateMapping.CalculationId, out CalculationCurrentVersion sourceCalculation))
                {
                    AddError($"Unable to locate source calculation for template mapping id {sourceTemplateMapping.TemplateId}");

                    continue;
                }

                if (!destinationTemplateCalculations.TryGetValue(destinationCalculationId, out CalculationCurrentVersion destinationCalculation))
                {
                    AddError($"Unable to locate destination calculation for template mapping id {sourceTemplateMapping.TemplateId}");

                    continue;
                }

                if (sourceCalculation.SourceCode.Trim().ToLowerInvariant() == destinationCalculation.SourceCode.Trim().ToLowerInvariant())
                {
                    continue;
                }

                WriteLine($"Updating template calculation source code in destination specification for calculation {destinationCalculation.Name}");

                string sourceCalculationId = sourceCalculation.Id;

                Calculation sourceCalculationDetails = await GetSourceCalculation(sourceCalculationId);

                if (MigrationHasErrors)
                {
                    continue;
                }

                //flag from command line to prevent writes and just test the configuration
                if (_preventWrites)
                {
                    continue;
                }

                await _destinationClients.MakeCalculationsCall(_ => _.EditCalculation(destinationSpecificationId,
                    destinationCalculationId,
                    new CalculationEditModel
                    {
                        SourceCode = sourceCalculation.SourceCode,
                        Description = sourceCalculationDetails.Description,
                        Name = destinationCalculation.Name,
                        ValueType = sourceCalculationDetails.Current.ValueType
                    }));
            }
        }

        private async Task<Calculation> GetSourceCalculation(string calculationId)
        {
            ApiResponse<Calculation> sourceCalculationDetailResponse = await _sourceClients.MakeCalculationsCall(_ => _.GetCalculationById(calculationId));

            CheckApiResponse(sourceCalculationDetailResponse, $"Unable to locate source calculation details for calculation {calculationId}");

            return sourceCalculationDetailResponse.Content;
        }

        private async Task CreateMissingAdditionalCalculations(CalculationCurrentVersion[] sourceAdditionalCalculations,
            CalculationCurrentVersion[] destinationAdditionalCalculations,
            string destinationSpecificationId)
        {
            foreach (CalculationCurrentVersion sourceAdditionalCalculation in sourceAdditionalCalculations)
            {
                string sourceCalculationName = sourceAdditionalCalculation.Name.Trim().ToLowerInvariant();
                string fundingStreamId = sourceAdditionalCalculation.FundingStreamId;

                try
                {
                    WriteLine($"Checking for source additional calculation {sourceCalculationName} in funding stream {fundingStreamId}");

                    if (destinationAdditionalCalculations.Any(_ => _.FundingStreamId == fundingStreamId &&
                                                                   _.Name.Trim().ToLowerInvariant() == sourceCalculationName))
                    {
                        continue;
                    }

                    WriteLine($"Creating missing additional calculation {sourceCalculationName} for funding stream {fundingStreamId}");

                    string sourceCalculationId = sourceAdditionalCalculation.Id;

                    Calculation sourceCalculationDetails = await GetSourceCalculation(sourceCalculationId);

                    if (MigrationHasErrors)
                    {
                        continue;
                    }

                    //flag from command line destination args to allow testing without writes to the destination system
                    if (_preventWrites)
                    {
                        continue;
                    }

                    await _destinationClients.MakeCalculationsCall(_ => _.CreateCalculation(destinationSpecificationId, new CalculationCreateModel
                    {
                        Description = sourceCalculationDetails.Description,
                        Name = sourceAdditionalCalculation.Name,
                        ValueType = sourceCalculationDetails.Current.ValueType,
                        SourceCode = sourceAdditionalCalculation.SourceCode
                    }));
                }
                catch (Exception exception)
                {
                    AddError($"Unable to create additional calculation {sourceCalculationName}");
                    AddError(exception.ToString());
                }
            }
        }

        private async Task<IEnumerable<TemplateMappingItem>> GetTemplateMappingItemsInSpecification(SpecificationSummary specification, MigrationClients clients)
        {
            SemaphoreSlim throttle = new SemaphoreSlim(5, 5);
            ConcurrentBag<Task<TemplateMapping>> templateMappingQueryTasks = new ConcurrentBag<Task<TemplateMapping>>();

            string specificationId = specification.Id;

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                templateMappingQueryTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        throttle.Wait();

                        string fundingStreamId = fundingStream.Id;

                        ApiResponse<TemplateMapping> templateMappingResponse = clients.MakeCalculationsCall(_ => _.GetTemplateMapping(specificationId, fundingStreamId))
                            .GetAwaiter()
                            .GetResult();

                        CheckApiResponse(templateMappingResponse, $"Unable to fetch template mapping for specification id {specificationId} and funding stream id {fundingStreamId}");

                        return templateMappingResponse.Content;
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }));
            }

            await TaskHelper.WhenAllAndThrow(templateMappingQueryTasks.ToArray());

            return templateMappingQueryTasks.SelectMany(_ => _.Result.TemplateMappingItems).ToArray();
        }

        private void CheckApiResponse<TDto>(ApiResponse<TDto> response, string errorMessage)
        {
            if (!response.StatusCode.IsSuccess() || response.Content == null)
            {
                AddError(errorMessage);
            }
        }

        private Task<ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>> GetSpecificationCurrentRelationshipsTask(string specificationId)
        {
            return _sourceClients.MakeDataSetsCall(_ => _.GetCurrentRelationshipsBySpecificationId(specificationId));
        }

        private Task<ApiResponse<SpecificationSummary>> GetSpecificationTask(string specificationId, MigrationClients migrationClients)
        {
            return migrationClients.MakeSpecificationsCall(_ => _.GetSpecificationSummaryById(specificationId));
        }

        private bool HaveDifferentTemplateVersions(SpecificationSummary source, SpecificationSummary destination)
        {
            /*
             * Ensure the template versions of specifications are the same between source and destination
             */

            return !(source.TemplateIds ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .SequenceEqual(destination.TemplateIds ?? Enumerable.Empty<KeyValuePair<string, string>>());
        }

        private async Task<bool> HaveIncompatibleDataSources(string sourceSpecificationId, string destinationSpecificationId)
        {
            Task<ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>> sourceRelationshipsTask =
                GetSpecificationCurrentRelationshipsTask(sourceSpecificationId);
            Task<ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>> destinationRelationshipsTask =
                GetSpecificationCurrentRelationshipsTask(destinationSpecificationId);

            await TaskHelper.WhenAllAndThrow(sourceRelationshipsTask, destinationRelationshipsTask);

            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> sourceResponse = sourceRelationshipsTask.Result;
            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> destinationResponse = destinationRelationshipsTask.Result;

            CheckApiResponse(sourceResponse, $"Did not locate current relationships for specification with id {sourceSpecificationId}");
            CheckApiResponse(destinationResponse, $"Did not locate current relationships for specification with id {destinationSpecificationId}");

            if (MigrationHasErrors)
            {
                WriteErrors();

                return true;
            }

            DataSourceNameAndSchemaComparer comparer = new DataSourceNameAndSchemaComparer();
            if (!sourceResponse.Content.All(_ => destinationResponse.Content.Contains(_, comparer)))
            {
                return true;
            }

            WriteLine("Checked source and destination data sources are compatible.");

            return false;
        }

        private class DataSourceNameAndSchemaComparer : IEqualityComparer<DatasetSpecificationRelationshipViewModel>
        {
            public bool Equals(DatasetSpecificationRelationshipViewModel x, DatasetSpecificationRelationshipViewModel y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null)
                {
                    return false;
                }

                if (y == null)
                {
                    return false;
                }

                return GetHashCode(x)
                    .Equals(GetHashCode(y));
            }

            public int GetHashCode(DatasetSpecificationRelationshipViewModel obj)
            {
                unchecked
                {
                    int hashCode = (obj.Name?.GetHashCode()).GetValueOrDefault();

                    hashCode = hashCode * 297 + (obj.Definition?.Id.GetHashCode()).GetValueOrDefault();

                    return hashCode;
                }
            }
        }

        private void PressAnyKeyToExit()
        {
            WriteLine("Press any key to exit:");
            ReadKey();
        }

        private void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        private void ReadKey()
        {
            Console.ReadKey();
        }

        private void AddError(string error)
        {
            _errors.Add(error);
        }

        private void WriteErrors()
        {
            foreach (string error in _errors)
            {
                WriteLine(error);
            }
        }

        private bool MigrationHasErrors => _errors.Any();
    }
}