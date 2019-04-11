using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDefinitionChangesDetectionService
    {
        DatasetDefinitionChanges DetectChanges(DatasetDefinition newDatasetDefinition, DatasetDefinition existingDatasetDefinition);
    }
}
