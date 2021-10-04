using System.Collections.Generic;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using DatasetRelationshipSummary = CalculateFunding.Models.Calcs.DatasetRelationshipSummary;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IDatasetReferenceService
    {
        IEnumerable<DatasetReference> GetDatasetRelationShips(IEnumerable<Calculation> calculations, IEnumerable<DatasetRelationshipSummary> datasetRelationShipSummary);
    }
}
