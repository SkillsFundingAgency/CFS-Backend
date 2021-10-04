using CalculateFunding.Services.Calcs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using DatasetRelationshipSummary = CalculateFunding.Models.Calcs.DatasetRelationshipSummary;
using DataField = CalculateFunding.Models.Graph.DataField;
using GraphDatasetRelationshipType = CalculateFunding.Models.Graph.DatasetRelationshipType;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Common.Utility;
using Serilog;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;

namespace CalculateFunding.Services.Calcs
{
    public class DatasetReferenceService : IDatasetReferenceService
    {
        private readonly ILogger _logger;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public DatasetReferenceService(
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            _logger = logger;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        public IEnumerable<DatasetReference> GetDatasetRelationShips(IEnumerable<Calculation> calculations, IEnumerable<DatasetRelationshipSummary> datasetRelationShipSummaries)
        {
            List<DatasetReference> datasetReferences = new List<DatasetReference>();

            calculations.Select(c => new { calculation = c, calcDatasetReferences = GetDataSetReferences(c.Current?.SourceCode) })
                .ForEach(x => x.calcDatasetReferences.ForEach(calcDatasetReference => AddDataSetReference(x.calculation, calcDatasetReference, datasetRelationShipSummaries, datasetReferences)));

            return datasetReferences;
        }

        private IEnumerable<string> GetDataSetReferences(string calculationSourceCode)
        {
            if (!string.IsNullOrWhiteSpace(calculationSourceCode))
            {
                MatchCollection datasetReferenceMatches = Regex.Matches(calculationSourceCode, "\\bdatasets((\\W|_)+)(\\w+)((\\W|_)+)(\\w+)\\b", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                if (datasetReferenceMatches.Count > 0)
                {
                    IEnumerable<string> datasetReferences = datasetReferenceMatches.Where(m => m.Groups.Count > 0).Select(x => x.Groups[0].Value);
                    IEnumerable<string> references = datasetReferences.Select(x => x.Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty))
                                    .Where(r => r.Count(c => c == '.') == 2 && !r.Split('.')[2].Equals("hasvalue", StringComparison.OrdinalIgnoreCase) )
                                    .Distinct()
                                    .ToList();

                    return references;
                }
            }

            return Enumerable.Empty<string>();
        }

        private void AddDataSetReference(Calculation calculation, string calcDatasetReference, IEnumerable<DatasetRelationshipSummary> datasetRelationShipSummaries, IList<DatasetReference> datasetReferences)
        {
            string[] parts = calcDatasetReference.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Count() == 3)
            {
                string datasetRelationshipName = parts[1]?.Trim().ToLowerInvariant();
                string fieldName = parts[2]?.Trim().ToLowerInvariant();

                if (datasetRelationShipSummaries != null)
                {
                    DatasetRelationshipSummary datasetRelationship = datasetRelationShipSummaries.FirstOrDefault(x => _typeIdentifierGenerator.GenerateIdentifier(x.Name).ToLowerInvariant() == datasetRelationshipName ||
                            _typeIdentifierGenerator.GenerateIdentifier(x.Name).Replace("_", string.Empty).ToLowerInvariant() == datasetRelationshipName.Replace("_", string.Empty)); // vb line continuation or names have underscore 

                    if (datasetRelationship != null && datasetRelationship.DatasetId != null)
                    {
                        FieldDefinition dataField = datasetRelationship.DatasetDefinition.TableDefinitions.FirstOrDefault()?.
                            FieldDefinitions.FirstOrDefault(x => _typeIdentifierGenerator.GenerateIdentifier(x.Name).ToLowerInvariant() == fieldName ||
                            _typeIdentifierGenerator.GenerateIdentifier(x.Name).ToLowerInvariant().Replace("_", string.Empty) == fieldName.Replace("_", string.Empty));

                        if (dataField == null)
                        {
                            _logger.Information($"A Datasetfield was not found: {dataField}");
                            return;
                        }

                        DatasetReference dataSetReference = datasetReferences.FirstOrDefault(x => x.PropertyName == datasetRelationship.Name && x.DataField?.DataFieldName == dataField.Name);

                        if (dataSetReference == null)
                        {
                            dataSetReference = new DatasetReference
                            {
                                PropertyName = datasetRelationship.Name,
                                Dataset = new CalculateFunding.Models.Graph.Dataset
                                {
                                    DatasetId = datasetRelationship.DatasetId,
                                    Name = datasetRelationship.Name,
                                    SpecificationId = calculation.SpecificationId,
                                },
                                DatasetDefinition = new CalculateFunding.Models.Graph.DatasetDefinition
                                {
                                    DatasetDefinitionId = datasetRelationship.DatasetDefinitionId,
                                    Description = datasetRelationship.DatasetDefinition.Description,
                                    Name = datasetRelationship.DatasetDefinition.Name,
                                    SpecificationId = calculation.SpecificationId
                                },
                                DataField = new DataField()
                                {
                                    DataFieldName = dataField.Name,
                                    DataFieldId = dataField.Id,
                                    DataFieldIsAggregable = dataField.IsAggregable,
                                    DatasetRelationshipId = datasetRelationship.Relationship?.Id,
                                    DataFieldRelationshipName = datasetRelationship.Relationship?.Name,
                                    SpecificationId = calculation.SpecificationId,
                                    CalculationId = calculation.Current.CalculationId,
                                    PropertyName = datasetRelationship.Name,
                                    SourceCodeName = calculation.Current.SourceCodeName,
                                    SchemaId = datasetRelationship.DatasetDefinitionId,
                                    SchemaFieldId = 
                                        datasetRelationship.RelationshipType == Models.Datasets.DatasetRelationshipType.Uploaded ?
                                        datasetRelationship.DatasetDefinitionId :
                                        datasetRelationship.Relationship?.Id
                                },
                                Calculations = new List<Models.Graph.Calculation>()
                                            {
                                                new Models.Graph.Calculation()
                                                {
                                                    SpecificationId = calculation.SpecificationId,
                                                    CalculationId = calculation.Current.CalculationId,
                                                    CalculationName = calculation.Name
                                                }
                                            },
                                DatasetRelationship = new Models.Graph.DatasetRelationship
                                {
                                    DatasetRelationshipId = datasetRelationship.Relationship?.Id,
                                    DatasetRelationshipName = datasetRelationship.Relationship?.Name,
                                    DatasetRelationshipType = (GraphDatasetRelationshipType)
                                        Enum.Parse(typeof(GraphDatasetRelationshipType), datasetRelationship.RelationshipType.ToString()),
                                    SchemaName = datasetRelationship.DatasetDefinition.Name,
                                    SchemaId = datasetRelationship.DatasetDefinitionId,
                                    SpecificationId = calculation.SpecificationId,
                                }
                            };

                            datasetReferences.Add(dataSetReference);
                        }
                        else
                        {
                            dataSetReference.Calculations.Add(new Models.Graph.Calculation()
                            {
                                SpecificationId = calculation.SpecificationId,
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