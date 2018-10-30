using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models;

namespace CalculateFunding.Repositories.Common.Cosmos.Interfaces
{
    public interface ICosmosRepository
    {
        IQueryable<T> Query<T>(string directSql = null, bool enableCrossPartitionQuery = false) where T : IIdentifiable;

        Task<HttpStatusCode> CreateAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable;

        Task<HttpStatusCode> UpsertAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable;

        IQueryable<dynamic> DynamicQuery<dynamic>(string sql, bool enableCrossPartitionQuery = false);

        IQueryable<dynamic> DynamicQueryPartionedEntity<dynamic>(string sql, string partitionEntityId = null);

        Task BulkCreateAsync<T>(IList<T> entities, int degreeOfParallelism = 5) where T : IIdentifiable;

        Task BulkCreateAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5) where T : IIdentifiable;

        Task<IEnumerable<T>> QueryPartitionedEntity<T>(string directSql, int itemsPerPage = -1, string partitionEntityId = null) where T : IIdentifiable;
    }
}
