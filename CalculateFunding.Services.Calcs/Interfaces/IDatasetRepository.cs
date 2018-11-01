using CalculateFunding.Models.Datasets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IDatasetRepository
    {
        Task<IEnumerable<DatasetSchemaRelationshipModel>> GetDatasetSchemaRelationshipModelsForSpecificationId(string specificationId);
    }
}
