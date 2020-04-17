using CalculateFunding.Services.Calcs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using DatasetRelationshipSummary = CalculateFunding.Models.Calcs.DatasetRelationshipSummary;
using DatasetField = CalculateFunding.Models.Graph.DatasetField;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Common.Utility;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class DatasetReferenceService : IDatasetReferenceService
    {
        private readonly ILogger _logger;

        public DatasetReferenceService(
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            _logger = logger;
        }

        public IEnumerable<DatasetReference> GetDatasetRelationShips(IEnumerable<Calculation> calculations, List<DatasetRelationshipSummary> datasetRelationShipSummaries)
        {
            List<DatasetReference> datasetReferences = new List<DatasetReference>();

            calculations
                .Where(x => x.FundingStreamId != "DSG" && x.Current?.CalculationType == CalculationType.Additional)
                .Select(c => new { calculation = c, calcDatasetReferences = GetDataSetReferences(c.Current?.SourceCode) })
                .ForEach(x => x.calcDatasetReferences.ForEach(calcDatasetReference => AddDataSetReference(x.calculation, calcDatasetReference, datasetRelationShipSummaries, datasetReferences)));

            return datasetReferences;
        }

        private IEnumerable<string> GetDataSetReferences(string calculationSourceCode)
        {
            if (!string.IsNullOrWhiteSpace(calculationSourceCode))
            {
                MatchCollection datasetReferenceMatches = Regex.Matches(calculationSourceCode, "\\bDatasets((\\W|_)+)(\\w+)((\\W|_)+)(\\w+)\\b", RegexOptions.Multiline | RegexOptions.Compiled);

                if (datasetReferenceMatches.Count > 0)
                {
                    IEnumerable<string> datasetReferences = datasetReferenceMatches.Where(m => m.Groups.Count > 0).Select(x => x.Groups[0].Value);
                    IEnumerable<string> references = datasetReferences.Select(x => x.Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty))
                                    .Where(r => r.Count(c => c == '.') == 2)
                                    .Distinct()
                                    .ToList();

                    return references;
                }
            }

            return Enumerable.Empty<string>();
        }

        private void AddDataSetReference(Calculation calculation, string calcDatasetReference, List<DatasetRelationshipSummary> datasetRelationShipSummaries, IList<DatasetReference> datasetReferences)
        {
            string[] parts = calcDatasetReference.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Count() == 3)
            {
                string datasetRelationshipName = parts[1]?.Trim();
                string fieldName = parts[2]?.Trim();

                if (datasetRelationShipSummaries != null)
                {
                    DatasetRelationshipSummary datasetRelationship = datasetRelationShipSummaries.FirstOrDefault(x => VisualBasicTypeGenerator.GenerateIdentifier(x.Name) == datasetRelationshipName ||
                            VisualBasicTypeGenerator.GenerateIdentifier(x.Name).Replace("_", string.Empty) == datasetRelationshipName.Replace("_", string.Empty)); // vb line continuation or names have underscore 

                    if (datasetRelationship != null)
                    {
                        FieldDefinition datasetField = datasetRelationship.DatasetDefinition.TableDefinitions.FirstOrDefault()?.
                            FieldDefinitions.FirstOrDefault(x => VisualBasicTypeGenerator.GenerateIdentifier(x.Name) == fieldName ||
                            VisualBasicTypeGenerator.GenerateIdentifier(x.Name).Replace("_", string.Empty) == fieldName.Replace("_", string.Empty));

                        if (datasetField == null)
                        {
                            _logger.Information($"A Datasetfield was not found: {datasetField}");
                            return;
                        }

                        DatasetReference dataSetReference = datasetReferences.FirstOrDefault(x => x.PropertyName == datasetRelationship.Name && x.DatasetField?.DatasetFieldName == datasetField.Name);

                        if (dataSetReference == null)
                        {
                            dataSetReference = new DatasetReference
                            {
                                PropertyName = datasetRelationship.Name,
                                DatasetField = new DatasetField()
                                {
                                    DatasetFieldName = datasetField.Name,
                                    DatasetFieldId = datasetField.Id,
                                    DatasetFieldIsAggregable = datasetField.IsAggregable,
                                    DatasetRelationshipId = datasetRelationship.Relationship?.Id,
                                    DatasetFieldRelatioshipName = datasetRelationship.Relationship?.Name,
                                    SpecificationId = calculation.SpecificationId,
                                    CalculationId = calculation.Current.CalculationId,
                                    PropertyName = datasetRelationship.Name
                                },
                                Calculations = new List<Models.Graph.Calculation>()
                                            {
                                                new Models.Graph.Calculation()
                                                {
                                                    CalculationId = calculation.Current.CalculationId,
                                                    CalculationName = calculation.Name
                                                }
                                            }
                            };

                            datasetReferences.Add(dataSetReference);
                        }
                        else
                        {
                            dataSetReference.Calculations.Add(new Models.Graph.Calculation()
                            {
                                CalculationId = calculation.Current.CalculationId,
                                CalculationName = calculation.Name
                            });
                        }
                    }
                }
            }
        }
    }
}