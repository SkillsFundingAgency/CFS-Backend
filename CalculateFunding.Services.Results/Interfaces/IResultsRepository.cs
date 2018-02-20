using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsRepository
    {
        //Task<HttpStatusCode> SaveDefinition(DatasetDefinition definition);

        //Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitions();

        //Task<IEnumerable<Dataset>> GetDatasetsByQuery(Expression<Func<Dataset, bool>> query);

        //Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitionsByQuery(Expression<Func<DatasetDefinition, bool>> query);

        //Task<HttpStatusCode> SaveDataset(Dataset dataset);

        //Task<HttpStatusCode> SaveDefinitionSpecificationRelationship(DefinitionSpecificationRelationship relationship);

        //Task<DatasetDefinition> GetDatasetDefinition(string definitionId);

        //Task<IEnumerable<DefinitionSpecificationRelationship>> GetDefinitionSpecificationRelationshipsByQuery(Expression<Func<DefinitionSpecificationRelationship, bool>> query);

        //Task<DefinitionSpecificationRelationship> GetRelationshipBySpecificationIdAndName(string specificationId, string name);
	    Task<ProviderResult> GetProviderResults(string providerId, string specificationId, string periodId);
    }
}
