using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionChangesDetectionService : IDefinitionChangesDetectionService
    {
        public DatasetDefinitionChanges DetectChanges(DatasetDefinition newDatasetDefinition, DatasetDefinition existingDatasetDefinition)
        {
            Guard.ArgumentNotNull(newDatasetDefinition, nameof(newDatasetDefinition));
            Guard.ArgumentNotNull(existingDatasetDefinition, nameof(existingDatasetDefinition));

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = newDatasetDefinition.Id
            };

            string newDatasetDefinitionAsJson = JsonConvert.SerializeObject(newDatasetDefinition);
            string existingDatasetDefinitionAsJson = JsonConvert.SerializeObject(existingDatasetDefinition);

            if(string.Equals(newDatasetDefinitionAsJson, existingDatasetDefinitionAsJson))
            {
                return datasetDefinitionChanges;
            }

            if (newDatasetDefinition.Name != existingDatasetDefinition.Name)
            {
                datasetDefinitionChanges.DefinitionChanges.Add(DefinitionChangeType.DefinitionName);
                datasetDefinitionChanges.NewName = newDatasetDefinition.Name;
            }

            datasetDefinitionChanges.TableDefinitionChanges.AddRange(DetectTableDefinitionChanges(existingDatasetDefinition.TableDefinitions, newDatasetDefinition.TableDefinitions));
           
            return datasetDefinitionChanges;
        }

        private IEnumerable<TableDefinitionChanges> DetectTableDefinitionChanges(IEnumerable<TableDefinition> existingTableDefinitions, IEnumerable<TableDefinition> newTableDefinitions)
        {
            IList<TableDefinitionChanges> allTableDefinitionChanges = new List<TableDefinitionChanges>();

            foreach (TableDefinition newTableDefinition in newTableDefinitions)
            {
                string tableDefinitionId = newTableDefinition.Id;

                TableDefinition existingTableDefinition = existingTableDefinitions.FirstOrDefault(m => m.Id == tableDefinitionId);

                TableDefinitionChanges tableDefinitionChanges = new TableDefinitionChanges
                {
                    TableDefinition = newTableDefinition,
                };

                if (existingTableDefinition != null)
                {
                    if (newTableDefinition.Name != existingTableDefinition.Name)
                    {
                        tableDefinitionChanges.ChangeTypes.Add(TableDefinitionChangeType.DefinitionName);
                    }

                    tableDefinitionChanges.FieldChanges.AddRange(DetectFieldDefinitionChanges(existingTableDefinition, newTableDefinition));

                    if (tableDefinitionChanges.HasChanges)
                    {
                        allTableDefinitionChanges.Add(tableDefinitionChanges);
                    }
                }
            }

            return allTableDefinitionChanges;
        }

        private IEnumerable<FieldDefinitionChanges> DetectFieldDefinitionChanges(TableDefinition existingTableDefinition, TableDefinition newTableDefinition)
        {
            List<FieldDefinitionChanges> allFieldDefinitionChanges = new List<FieldDefinitionChanges>();

            IEnumerable<FieldDefinition> newFieldDefinitions = newTableDefinition.FieldDefinitions;

            IEnumerable<FieldDefinition> existingFieldDefinitions = existingTableDefinition.FieldDefinitions;

            IEnumerable<FieldDefinitionChanges> extraFieldDefinitionChanges = newFieldDefinitions.Except(existingFieldDefinitions, new FieldDefinitionComparer())
                .Select(m => new FieldDefinitionChanges(FieldDefinitionChangeType.AddedField)
                {
                    FieldDefinition = m,
                });

            allFieldDefinitionChanges.AddRange(extraFieldDefinitionChanges);

            IEnumerable<FieldDefinitionChanges> removedFieldDefinitionChanges = existingFieldDefinitions.Except(newFieldDefinitions, new FieldDefinitionComparer())
               .Select(m => new FieldDefinitionChanges(FieldDefinitionChangeType.RemovedField)
               {
                   FieldDefinition = m
               });

            allFieldDefinitionChanges.AddRange(removedFieldDefinitionChanges);

            foreach (FieldDefinition newFieldDefinition in newFieldDefinitions)
            {
                string newFieldDefinitionId = newFieldDefinition.Id;

                if (extraFieldDefinitionChanges.AnyWithNullCheck(m => m.FieldDefinition.Id == newFieldDefinitionId) || removedFieldDefinitionChanges.AnyWithNullCheck(m => m.FieldDefinition.Id == newFieldDefinitionId))
                {
                    continue;
                }

                FieldDefinition existingFieldDefinition = existingFieldDefinitions.FirstOrDefault(m => m.Id == newFieldDefinitionId);

                FieldDefinitionChanges fieldDefinitionChanges = new FieldDefinitionChanges
                {
                    FieldDefinition = newFieldDefinition,
                };

                if (existingFieldDefinition != null)
                {
                    if (!string.Equals(newFieldDefinition.Name, existingFieldDefinition.Name))
                    {
                        fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.FieldName);
                    }

                    if (newFieldDefinition.IsAggregable && !existingFieldDefinition.IsAggregable)
                    {
                        fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.IsAggregable);
                    }

                    if (!newFieldDefinition.IsAggregable && existingFieldDefinition.IsAggregable)
                    {
                        fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.IsNotAggregable);
                    }

                    if(newFieldDefinition.Type != existingFieldDefinition.Type)
                    {
                        fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.FieldType);
                    }

                    if(newFieldDefinition.IdentifierFieldType != existingFieldDefinition.IdentifierFieldType)
                    {
                        fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.IdentifierType);
                    }
                }

                if (fieldDefinitionChanges.HasChanges)
                {
                    allFieldDefinitionChanges.Add(fieldDefinitionChanges);
                }
            }

            return allFieldDefinitionChanges;
        }
    }
}
